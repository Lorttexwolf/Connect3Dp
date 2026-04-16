using Lib3Dp.Configuration;
using Lib3Dp.Constants;
using Lib3Dp.Extensions;
using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace Lib3Dp.Connectors.ELEGOO
{
	[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
	public enum ELEGOOMachineKind
	{
		CentauriCarbon,
		CentauriCarbon2,
	}

	/// <param name="IPAddress">Printer host: IPv4, IPv6, or hostname. Optional <c>http://</c>/<c>https://</c>, path, or <c>:3030</c> are stripped when connecting.</param>
	public record ELEGOOMachineConfiguration(
		string? Nickname,
		ELEGOOMachineKind Model,
		string SerialNumber,
		string IPAddress,
		Material? LoadedMaterial = null
	);

	public class ELEGOOMachineConnector : MachineConnection, IConfigurableConnection<ELEGOOMachineConnector, ELEGOOMachineConfiguration>
	{
		private SimpleWebSocketClient? Socket;
		private readonly Logger Log;
		private readonly HttpClient Http;

		private string? MainboardID;
		private Timer? HeartbeatTimer;
		private Timer? StatusPollTimer;

		private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> PendingResponses = new();

		public ELEGOOMachineConfiguration Configuration { get; private set; }

		public ELEGOOMachineConnector(IMachineFileStore fileStore, ELEGOOMachineConfiguration config)
			: base(fileStore, new MachineConnectionConfiguration(
				config.Nickname,
				$"ELEGOO{config.SerialNumber}",
				"ELEGOO",
				ELEGOOConstants.GetModelName(config.Model)))
		{
			this.Configuration = config;
			this.Log = Logger.OfCategory($"ELEGOO {config.Nickname ?? config.SerialNumber}");
			var host = NormalizePrinterHost(config.IPAddress);
			this.Http = new HttpClient
			{
				BaseAddress = new Uri($"http://{host}:3030/"),
				Timeout = TimeSpan.FromMinutes(5)
			};
		}

		#region Configuration

		public override ELEGOOMachineConfiguration GetConfiguration() => this.Configuration;

		public async Task<MachineOperationResult> UpdateConfiguration(ELEGOOMachineConfiguration updatedCfg)
		{
			var opResult = await Mono.MutateUntil(async () =>
			{
				await DisconnectSocket();
				this.Configuration = updatedCfg;
				this.Http.BaseAddress = new Uri($"http://{NormalizePrinterHost(updatedCfg.IPAddress)}:3030/");
				await Connect_Internal();
			}, () => this.State.Status is not MachineStatus.Disconnected, TimeSpan.FromSeconds(30));

			return opResult.IntoOperationResult("elegoo.config.update.failed", "Update Configuration");
		}

		public static Type GetConfigurationType() => typeof(ELEGOOMachineConfiguration);

		public static ELEGOOMachineConnector CreateFromConfiguration(IMachineFileStore fileStore, ELEGOOMachineConfiguration configuration)
		{
			return new ELEGOOMachineConnector(fileStore, configuration);
		}

		#endregion

		#region Connect / Disconnect

		protected override async Task<MachineOperationResult> Connect_Internal()
		{
			try
			{
				var wsUri = new Uri($"ws://{Configuration.IPAddress}:3030/websocket");
				Socket = new SimpleWebSocketClient(wsUri);

				Socket.OnMessage += OnWebSocketMessage;
				Socket.OnDisconnected += OnWebSocketDisconnected;

				await Socket.ConnectAsync();

				Log.Info($"WebSocket connected to {wsUri}");

				// SDCP heartbeat: send "ping" every 30 seconds to keep connection alive
				HeartbeatTimer = new Timer(async _ =>
				{
					try
					{
						if (Socket?.State == WebSocketState.Open)
							await Socket.SendTextAsync("ping");
					}
					catch { /* Heartbeat failure triggers OnDisconnected */ }
				}, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

				// Set initial connected state so MutateUntil predicate is satisfied.
				// Seed "Chamber" so MachineConnection.ToggleLight does not fail with
				// "fixture does not exist" before the first status carrying LightStatus arrives.
				this.CommitState(update =>
				{
					update.SetStatus(MachineStatus.Idle);
					update.SetCapabilities(ELEGOOConstants.GetCapabilities(Configuration.Model));
					update.SetLights("Chamber", false);

					// Register the framework-managed filament slot. ELEGOO printers do not report
					// what material is loaded; the user tracks it here via ChangeMaterial.
					update.UpdateMaterialUnits("External", mu => mu
						.SetCapacity(1)
						.SetCapabilities(MUCapabilities.ModifyTray));

					if (Configuration.LoadedMaterial is { } mat)
						update.UpdateMaterialUnits("External", mu => mu
							.UpdateTrays(0, t => t.UpdateMaterial(m => m
								.SetName(mat.Name)
								.SetColor(mat.Color)
								.SetFProfileIDX(mat.FProfileIDX))));

					update.UpdateExtruders(0, e => e.SetLoadedSpool(new SpoolLocation("External", 0)));
				});

				// Request printer attributes (gives us MainboardID) and initial status
				await SendCommand(ELEGOOCmd.GetAttributes);
				await SendCommand(ELEGOOCmd.GetStatus);

				// Poll status periodically as a safety net alongside printer-pushed updates
				StatusPollTimer = new Timer(async _ =>
				{
					try
					{
						if (Socket?.State == WebSocketState.Open)
							await SendCommand(ELEGOOCmd.GetStatus);
					}
					catch { }
				}, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));

				return MachineOperationResult.Ok;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to connect to ELEGOO printer at {NormalizePrinterHost(Configuration.IPAddress)} (from config: {Configuration.IPAddress})");

				return MachineOperationResult.Fail(
					"elegoo.connect.failed",
					"Unable to Connect",
					ex.Message,
					MachineMessageActions.CheckConfiguration,
					new MachineMessageAutoResole { WhenConnected = true });
			}
		}

		public override async Task Disconnect()
		{
			await DisconnectSocket();

			this.CommitState(update =>
			{
				update.SetStatus(MachineStatus.Disconnected);
				update.SetCapabilities(MachineCapabilities.None);
				update.UnsetCurrentJob();
			});
		}

		private async Task DisconnectSocket()
		{
			HeartbeatTimer?.Dispose();
			HeartbeatTimer = null;
			StatusPollTimer?.Dispose();
			StatusPollTimer = null;

			if (Socket != null)
			{
				Socket.OnMessage -= OnWebSocketMessage;
				Socket.OnDisconnected -= OnWebSocketDisconnected;

				try { await Socket.DisposeAsync(); }
				catch { }

				Socket = null;
			}

			MainboardID = null;

			foreach (var (_, tcs) in PendingResponses)
				tcs.TrySetCanceled();
			PendingResponses.Clear();
		}

		#endregion

		#region WebSocket Message Handling

		private void OnWebSocketDisconnected(WebSocketCloseStatus? status, string? description)
		{
			Log.Info($"WebSocket disconnected: {status} — {description}");

			this.CommitState(update =>
			{
				update.SetStatus(MachineStatus.Disconnected);
				update.SetCapabilities(MachineCapabilities.None);
				update.UnsetCurrentJob();
			});
		}

		private void OnWebSocketMessage(string message)
		{
			if (string.IsNullOrWhiteSpace(message) || message == "pong")
				return;

			try
			{
				using var doc = JsonDocument.Parse(message);
				var root = doc.RootElement;

				if (root.TryGetProperty("Topic", out var topicEl))
				{
					string? topic = topicEl.GetString();
					if (topic != null)
					{
						if (topic.Contains("sdcp/status")) HandleStatusMessage(root);
						else if (topic.Contains("sdcp/attributes")) HandleAttributesMessage(root);
						else if (topic.Contains("sdcp/response")) HandleResponseMessage(root);
						else if (topic.Contains("sdcp/error")) HandleErrorMessage(root);
					}
				}
				else if (root.TryGetProperty("Status", out _))
				{
					HandleStatusMessage(root);
				}
				else if (root.TryGetProperty("Attributes", out _))
				{
					HandleAttributesMessage(root);
				}
				else if (root.TryGetProperty("Data", out var dataEl) && dataEl.TryGetProperty("Cmd", out _))
				{
					HandleResponseMessage(root);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to parse incoming WebSocket message");
			}
		}

		private void HandleAttributesMessage(JsonElement root)
		{
			if (!root.TryGetProperty("Attributes", out var attrs))
				return;

			bool isFirstMainboardID = MainboardID == null;

			if (attrs.TryGetProperty("MainboardID", out var mbIdEl))
			{
				var mbId = mbIdEl.GetString();
				if (!string.IsNullOrEmpty(mbId))
				{
					MainboardID = mbId;
					Log.Info($"MainboardID acquired: {MainboardID}");
				}
			}

			if (attrs.TryGetProperty("Name", out var nameEl))
			{
				var name = nameEl.GetString();
				if (!string.IsNullOrEmpty(name))
					this.CommitState(u => u.SetNickname(name));
			}

			// Once MainboardID is known, request the file list (requires the ID)
			if (isFirstMainboardID && MainboardID != null)
			{
				_ = Task.Run(async () =>
				{
					try { await SendCommand(ELEGOOCmd.GetFileList, new { Url = "/local" }); }
					catch (Exception ex) { Log.Error(ex, "Failed to request initial file list"); }
				});
			}
		}

		private void HandleStatusMessage(JsonElement root)
		{
			if (!root.TryGetProperty("Status", out var status))
				return;

			// Extract MainboardID if present in status (some firmware versions include it)
			if (MainboardID == null
				&& status.TryGetProperty("MainboardID", out var mbIdEl)
				&& mbIdEl.GetString() is string mbId
				&& !string.IsNullOrEmpty(mbId))
			{
				MainboardID = mbId;
				Log.Info($"MainboardID from status: {MainboardID}");

				_ = Task.Run(async () =>
				{
					try { await SendCommand(ELEGOOCmd.GetFileList, new { Url = "/local" }); }
					catch { }
				});
			}

			var stateUpdate = new MachineStateUpdate();

			MapTemperatures(status, stateUpdate);
			MapFans(status, stateUpdate);
			MapLights(status, stateUpdate);
			MapPrintInfo(status, stateUpdate);

			stateUpdate.SetCapabilities(ELEGOOConstants.GetCapabilities(Configuration.Model));

			this.CommitState(stateUpdate);
		}

		private static void MapTemperatures(JsonElement status, MachineStateUpdate update)
		{
			if (status.TryGetProperty("TempOfHotbed", out var bedTemp))
			{
				double target = status.TryGetProperty("TempTargetHotbed", out var bt) ? bt.GetDouble() : 0;
				update.SetHeatingElements(HeatingElementNames.Bed,
					new HeatingElement(bedTemp.GetDouble(), target, ELEGOOConstants.BedConstraints));
			}

			if (status.TryGetProperty("TempOfNozzle", out var nozzleTemp))
			{
				double target = status.TryGetProperty("TempTargetNozzle", out var nt) ? nt.GetDouble() : 0;
				update.SetHeatingElements(HeatingElementNames.Nozzle,
					new HeatingElement(nozzleTemp.GetDouble(), target, ELEGOOConstants.NozzleConstraints));
			}

			if (status.TryGetProperty("TempOfBox", out var chamberTemp))
			{
				double target = status.TryGetProperty("TempTargetBox", out var ct) ? ct.GetDouble() : 0;
				update.SetHeatingElements(HeatingElementNames.Chamber,
					new HeatingElement(chamberTemp.GetDouble(), target, ELEGOOConstants.ChamberConstraints));
			}
		}

		private static void MapFans(JsonElement status, MachineStateUpdate update)
		{
			if (!status.TryGetProperty("CurrentFanSpeed", out var fans))
				return;

			if (fans.TryGetProperty("ModelFan", out var mf))
				update.SetFans("ModelFan", mf.GetInt32());
			if (fans.TryGetProperty("AuxiliaryFan", out var af))
				update.SetFans("AuxiliaryFan", af.GetInt32());
			if (fans.TryGetProperty("BoxFan", out var bf))
				update.SetFans("BoxFan", bf.GetInt32());
		}

		private static void MapLights(JsonElement status, MachineStateUpdate update)
		{
			if (!TryGetPropertyIgnoreCase(status, "LightStatus", out var lights))
				return;

			if (lights.ValueKind == JsonValueKind.Object)
			{
				if (!TryGetPropertyIgnoreCase(lights, "SecondLight", out var sl))
					return;
				bool isOn = sl.ValueKind switch
				{
					JsonValueKind.True => true,
					JsonValueKind.False => false,
					JsonValueKind.Number => sl.TryGetInt32(out var i) ? i != 0 : sl.GetDouble() != 0,
					_ => false
				};
				update.SetLights("Chamber", isOn);
			}
			else if (lights.ValueKind == JsonValueKind.Number)
			{
				update.SetLights("Chamber", lights.TryGetInt32(out var n) ? n != 0 : lights.GetDouble() != 0);
			}
			else if (lights.ValueKind is JsonValueKind.True or JsonValueKind.False)
			{
				update.SetLights("Chamber", lights.ValueKind == JsonValueKind.True);
			}
		}

		private static bool TryGetPropertyIgnoreCase(JsonElement parent, string name, out JsonElement value)
		{
			foreach (var p in parent.EnumerateObject())
			{
				if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					value = p.Value;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// SDCP V3 uses <c>CurrentTicks</c> / <c>TotalTicks</c> for elapsed and estimated total print duration (may be fractional).
		/// Most firmware uses milliseconds; Centauri FDM often uses seconds when values stay below ~100k. Values in the millions are ms.
		/// </summary>
		private static bool TryGetPrintTickDurations(JsonElement printInfo, out double current, out double total, out bool ticksAreMilliseconds)
		{
			current = 0;
			total = 0;
			ticksAreMilliseconds = true;
			bool haveCurrent = TryGetPropertyIgnoreCase(printInfo, "CurrentTicks", out var c)
				&& c.ValueKind == JsonValueKind.Number;
			bool haveTotal = TryGetPropertyIgnoreCase(printInfo, "TotalTicks", out var t)
				&& t.ValueKind == JsonValueKind.Number;
			if (haveCurrent)
				current = c.TryGetInt64(out var cl) ? cl : c.GetDouble();
			if (haveTotal)
				total = t.TryGetInt64(out var tl) ? tl : t.GetDouble();
			if (!(haveTotal && total > 0))
				return false;
			// Same heuristic as community integrations: small totals behave like seconds on FDM; large totals are ms (multi-hour jobs).
			ticksAreMilliseconds = total >= 100_000;
			return true;
		}

		private static bool TryGetSecondsTimeSpan(JsonElement el, out TimeSpan span)
		{
			span = TimeSpan.Zero;
			if (el.ValueKind != JsonValueKind.Number)
				return false;
			double sec = el.TryGetInt64(out var li) ? li : el.GetDouble();
			if (sec < 0)
				return false;
			span = TimeSpan.FromSeconds(sec);
			return true;
		}

		private void MapPrintInfo(JsonElement status, MachineStateUpdate stateUpdate)
		{
			int currentStatus = 0;
			if (status.TryGetProperty("CurrentStatus", out var csArr) && csArr.GetArrayLength() > 0)
				currentStatus = csArr[0].GetInt32();

			if (!status.TryGetProperty("PrintInfo", out var printInfo))
			{
				// No PrintInfo — if we were previously printing, transition through Printed
				// so the base class CommitState can record history
				if (this.State.Status is MachineStatus.Printing && this.State.CurrentJob is not null)
					stateUpdate.SetStatus(MachineStatus.Printed);
				else
					stateUpdate.SetStatus(MachineStatus.Idle);

				return;
			}

			int printStatus = printInfo.TryGetProperty("Status", out var psEl) ? psEl.GetInt32() : 0;
			var mappedStatus = ELEGOOConstants.MapPrintStatus(printStatus);

			stateUpdate.SetStatus(mappedStatus);

			bool isActive = mappedStatus is MachineStatus.Printing or MachineStatus.Paused;
			bool justEnded = mappedStatus is MachineStatus.Printed or MachineStatus.Canceled;

			if (isActive || justEnded)
			{
				string filename = printInfo.TryGetProperty("Filename", out var fnEl)
					? (fnEl.GetString() ?? "") : "";

				int currentLayer = printInfo.TryGetProperty("CurrentLayer", out var clEl) ? clEl.GetInt32() : 0;
				int totalLayer = printInfo.TryGetProperty("TotalLayer", out var tlEl) ? tlEl.GetInt32() : 0;

				bool haveTicks = TryGetPrintTickDurations(printInfo, out var currentTickRaw, out var totalTickRaw, out var tickMs);

				int progress = 0;
				if (haveTicks)
					progress = (int)Math.Clamp(Math.Round(currentTickRaw * 100.0 / totalTickRaw), 0, 100);
				else if (printInfo.TryGetProperty("Progress", out var progEl) && progEl.ValueKind == JsonValueKind.Number)
					progress = progEl.TryGetInt32(out var pi) ? pi : (int)progEl.GetDouble();

				if (progress == 0 && totalLayer > 0)
					progress = (int)(currentLayer * 100.0 / totalLayer);

				if (justEnded && mappedStatus is MachineStatus.Printed)
					progress = 100;

				TimeSpan remainingTime = TimeSpan.Zero;
				TimeSpan totalTime = TimeSpan.Zero;
				if (haveTicks)
				{
					double remainingRaw = Math.Max(0, totalTickRaw - currentTickRaw);
					if (tickMs)
					{
						totalTime = TimeSpan.FromMilliseconds(totalTickRaw);
						remainingTime = TimeSpan.FromMilliseconds(remainingRaw);
					}
					else
					{
						totalTime = TimeSpan.FromSeconds(totalTickRaw);
						remainingTime = TimeSpan.FromSeconds(remainingRaw);
					}
				}
				else
				{
					if (TryGetPropertyIgnoreCase(printInfo, "RemainingTime", out var remEl))
						TryGetSecondsTimeSpan(remEl, out remainingTime);
					if (TryGetPropertyIgnoreCase(printInfo, "TotalTime", out var totEl))
						TryGetSecondsTimeSpan(totEl, out totalTime);
				}

				if (totalTime == TimeSpan.Zero && remainingTime > TimeSpan.Zero && progress > 0)
					totalTime = TimeSpan.FromSeconds(remainingTime.TotalSeconds / (1.0 - progress / 100.0));

				string jobName = Path.GetFileNameWithoutExtension(filename);

				string displayName = string.IsNullOrEmpty(jobName) ? "Unknown" : jobName;
				string? subStage = totalLayer > 0 ? $"Layer {currentLayer}/{totalLayer}" : null;
				string? customID = printInfo.TryGetProperty("TaskId", out var taskIdEl)
					? taskIdEl.GetString() : null;

				stateUpdate.UpdateCurrentJob(job =>
				{
					job.SetName(displayName);
					job.SetPercentageComplete(Math.Clamp(progress, 0, 100));
					job.SetRemainingTime(remainingTime);
					job.SetTotalTime(totalTime);
					job.SetLocalPath(filename);
					if (subStage != null) job.SetSubStage(subStage);
					if (customID != null) job.SetCustomID(customID);
				});
			}
			else
			{
				stateUpdate.UnsetCurrentJob();
			}
		}

		private void HandleResponseMessage(JsonElement root)
		{
			if (!root.TryGetProperty("Data", out var data))
				return;

			int cmd = data.TryGetProperty("Cmd", out var cmdEl) ? cmdEl.GetInt32() : -1;

			if (data.TryGetProperty("Data", out var responseData))
			{
				// Complete any pending awaitable response
				if (PendingResponses.TryRemove(cmd, out var tcs))
					tcs.TrySetResult(responseData.Clone());

				switch (cmd)
				{
					case ELEGOOCmd.GetFileList:
						HandleFileListResponse(responseData);
						break;
					case ELEGOOCmd.GetHistoryIds:
						HandleHistoryIdsResponse(responseData);
						break;
					case ELEGOOCmd.GetTaskDetail:
						HandleTaskDetailResponse(responseData);
						break;
				}
			}
		}

		private void HandleFileListResponse(JsonElement data)
		{
			if (!data.TryGetProperty("FileList", out var fileList))
				return;

			var stateUpdate = new MachineStateUpdate();

			foreach (var existing in this.State.LocalJobs)
				stateUpdate.RemoveLocalJobs(existing);

			foreach (var file in fileList.EnumerateArray())
			{
				string name = file.TryGetProperty("name", out var n) ? (n.GetString() ?? "") : "";
				long size = file.TryGetProperty("size", out var s) ? s.GetInt64() : 0;

				if (string.IsNullOrEmpty(name)) continue;

				// The file list response returns bare filenames (e.g. "model.gcode") relative to the
				// requested Url. StartPrint requires the full path ("/local/model.gcode" or "/usb/…").
				// Guard against firmware that already returns a full path.
				string uri = name.StartsWith('/') ? name : $"/local/{name}";

				string hash = Convert.ToHexStringLower(
					SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{name}:{size}")));

				var fileHandle = new MachineFileHandle(this.ID, uri, "application/gcode", hash);
				var localJob = new LocalPrintJob(
					Path.GetFileNameWithoutExtension(name),
					fileHandle,
					0,
					TimeSpan.Zero,
					new Dictionary<int, MaterialToPrint>());

				stateUpdate.SetLocalJobs(localJob);
			}

			stateUpdate.SetCapabilities(this.State.Capabilities | MachineCapabilities.LocalJobs);
			this.CommitState(stateUpdate);

			Log.Info($"File list refreshed: {fileList.GetArrayLength()} files");
		}

		private void HandleHistoryIdsResponse(JsonElement data)
		{
			if (!data.TryGetProperty("HistoryIdList", out var idList))
				return;

			var ids = new List<string>();
			foreach (var id in idList.EnumerateArray())
			{
				if (id.GetString() is string val)
					ids.Add(val);
			}

			if (ids.Count > 0)
			{
				_ = Task.Run(async () =>
				{
					try { await SendCommand(ELEGOOCmd.GetTaskDetail, new { Id = ids }); }
					catch (Exception ex) { Log.Error(ex, "Failed to request task details"); }
				});
			}
		}

		private void HandleTaskDetailResponse(JsonElement data)
		{
			if (!data.TryGetProperty("HistoryDetailList", out var details))
				return;

			var stateUpdate = new MachineStateUpdate();

			foreach (var detail in details.EnumerateArray())
			{
				string taskName = detail.TryGetProperty("TaskName", out var tn)
					? (tn.GetString() ?? "Unknown") : "Unknown";
				int taskStatus = detail.TryGetProperty("Status", out var ts)
					? ts.GetInt32() : 0;

				bool completed = taskStatus == ELEGOOPrintStatus.Completed;

				stateUpdate.SetJobHistory(new HistoricPrintJob(
					Path.GetFileNameWithoutExtension(taskName),
					completed,
					DateTime.Now,
					TimeSpan.Zero,
					null,
					null));
			}

			this.CommitState(stateUpdate);
		}

		private void HandleErrorMessage(JsonElement root)
		{
			string errorDetail = "Unknown printer error";
			if (root.TryGetProperty("Data", out var data))
				errorDetail = data.ToString();

			Log.Error(null!, $"Printer error received: {errorDetail}");

			this.AddNotification(new MachineMessage(
				"elegoo.printer.error",
				"Printer Error",
				errorDetail,
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default));
		}

		#endregion

		#region Command Sending

		/// <summary>
		/// Builds and sends an SDCP-formatted WebSocket message.
		/// </summary>
		private async Task SendCommand(int cmd, object? data = null)
		{
			if (Socket == null || Socket.State != WebSocketState.Open)
				throw new InvalidOperationException("WebSocket is not connected");

			var message = new
			{
				Id = "",
				Data = new
				{
					Cmd = cmd,
					Data = data ?? new { },
					RequestID = Guid.NewGuid().ToString("N"),
					MainboardID = MainboardID ?? "",
					TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
					From = 1
				}
			};

			var json = JsonSerializer.Serialize(message);
			await Socket.SendTextAsync(json);
		}

		/// <summary>
		/// Sends a command and waits for the printer's response (matched by Cmd).
		/// </summary>
		private async Task<JsonElement> SendCommandWithResponse(int cmd, object? data = null, TimeSpan? timeout = null)
		{
			var tcs = new TaskCompletionSource<JsonElement>();
			PendingResponses[cmd] = tcs;

			await SendCommand(cmd, data);

			using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
			cts.Token.Register(() => tcs.TrySetCanceled());

			return await tcs.Task;
		}

		#endregion

		#region Printer Control — Pause / Resume / Stop / Print / Lights / ClearBed

		protected override async Task Pause_Internal()
		{
			await SendCommand(ELEGOOCmd.PausePrint);
		}

		protected override async Task Resume_Internal()
		{
			await SendCommand(ELEGOOCmd.ResumePrint);
		}

		protected override async Task Stop_Internal()
		{
			await SendCommand(ELEGOOCmd.StopPrint);
		}

		protected override Task ClearBed_Internal()
		{
			this.CommitState(update =>
			{
				update.SetStatus(MachineStatus.Idle);
				update.UnsetCurrentJob();
			});
			return Task.CompletedTask;
		}

		protected override Task<MachineOperationResult> Invoke_ChangeMaterial(SpoolLocation location, Material material)
		{
			this.CommitState(update =>
				update.UpdateMaterialUnits(location.MUID, mu =>
					mu.UpdateTrays(location.Slot, t => t.UpdateMaterial(m => m
						.SetName(material.Name)
						.SetColor(material.Color)
						.SetFProfileIDX(material.FProfileIDX)))));

			this.Configuration = this.Configuration with { LoadedMaterial = material };
			this.NotifyConfigurationChanged();

			return Task.FromResult(MachineOperationResult.Ok);
		}

		protected override async Task<MachineOperationResult> UploadFileToMachine(MachineFileHandle handle, Stream stream)
		{
			var fileName = Path.GetFileName(handle.URI);
			return await UploadFile(stream, fileName);
		}

		protected override async Task PrintLocal_Internal(LocalPrintJob localPrint, PrintOptions options)
		{
			await SendCommand(ELEGOOCmd.StartPrint, new
			{
				Filename = localPrint.File.URI,
				StartLayer = 0,
				Calibration_switch = options.LevelBed ? 1 : 0,
				PrintPlatformType = 0,
				Tlp_Switch = 0
			});
		}

		protected override async Task ToggleLight_Internal(string fixtureName, bool isOn)
		{
			await SendCommand(ELEGOOCmd.EditStatusData, new
			{
				LightStatus = new
				{
					SecondLight = isOn ? 1 : 0,
					RgbLight = isOn ? new[] { 255, 255, 255 } : new[] { 0, 0, 0 }
				}
			});

			// Firmware often does not push LightStatus quickly (or before the next poll); base
			// ToggleLight waits on state. Apply the value we commanded so MutateUntil succeeds.
			if (fixtureName == "Chamber")
				this.CommitState(u => u.SetLights("Chamber", isOn));
		}

		// TODO: Fan capability model — some fans (BoxFan) are binary (0 = off, any non-zero = on)
		// while others (ModelFan, AuxiliaryFan) support a 0–100 percentage. Future work: introduce
		// a per-fan capability flag (e.g. FanMode.OnOff vs FanMode.Percentage) so the framework
		// can validate/clamp appropriately per fan rather than treating all fans as percentage-based.
		protected override async Task SetFanSpeed_Internal(string fanName, int speedPercent)
		{
			var model = State.Fans.GetValueOrDefault("ModelFan", 0);
			var aux = State.Fans.GetValueOrDefault("AuxiliaryFan", 0);
			var box = State.Fans.GetValueOrDefault("BoxFan", 0);

			switch (fanName)
			{
				case "ModelFan":
					model = speedPercent;
					break;
				case "AuxiliaryFan":
					aux = speedPercent;
					break;
				case "BoxFan":
					box = speedPercent;
					break;
				default:
					await SendCommand(ELEGOOCmd.EditStatusData, new
					{
						TargetFanSpeed = new Dictionary<string, int> { { fanName, speedPercent } }
					});
					this.CommitState(u => u.SetFans(fanName, speedPercent));
					return;
			}

			await SendCommand(ELEGOOCmd.EditStatusData, new
			{
				TargetFanSpeed = new
				{
					ModelFan = model,
					AuxiliaryFan = aux,
					BoxFan = box
				}
			});

			this.CommitState(u =>
			{
				u.SetFans("ModelFan", model);
				u.SetFans("AuxiliaryFan", aux);
				u.SetFans("BoxFan", box);
			});
		}

		#endregion

		#region File Operations (HTTP Upload + WebSocket Management)

		/// <summary>
		/// Uploads a file to the printer's local storage via HTTP multipart POST.
		/// Large files are sent in chunks using the Offset field since the
		/// printer's HTTP server rejects bodies above ~4 MB.
		/// </summary>
		public async Task<MachineOperationResult> UploadFile(Stream fileStream, string fileName, bool toUSB = false)
		{
			const int ChunkSize = 1 * 1024 * 1024;

			try
			{
				string md5Hash;
				using (var md5 = MD5.Create())
				{
					var hashBytes = await md5.ComputeHashAsync(fileStream);
					md5Hash = Convert.ToHexStringLower(hashBytes);
					fileStream.Position = 0;
				}

				var endpoint = toUSB ? "/uploadFile/uploadUSB" : "/uploadFile/upload";
				var totalSize = fileStream.Length;
				var uuid = Guid.NewGuid().ToString("N");
				long offset = 0;
				var buffer = new byte[ChunkSize];

				while (offset < totalSize)
				{
					var bytesToRead = (int)Math.Min(ChunkSize, totalSize - offset);
					var bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, bytesToRead));

					using var content = new MultipartFormDataContent();
					content.Add(new StringContent("0"), "Check");
					content.Add(new StringContent(offset.ToString()), "Offset");
					content.Add(new StringContent(totalSize.ToString()), "TotalSize");
					content.Add(new StringContent(uuid), "Uuid");

					var fileContent = new ByteArrayContent(buffer, 0, bytesRead);
					fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
					content.Add(fileContent, "File", fileName);
					content.Add(new StringContent(md5Hash), "S-File-MD5");

					var response = await Http.PostAsync(endpoint, content);
					response.EnsureSuccessStatusCode();

					offset += bytesRead;
				}

				await RefreshFileList();

				Log.Info($"Uploaded file: {fileName}");
				return MachineOperationResult.Ok;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to upload file: {fileName}");
				return MachineOperationResult.Fail("elegoo.upload.failed", "File Upload Failed", ex.Message);
			}
		}

		/// <summary>
		/// Deletes a file from the printer's storage via WebSocket command.
		/// </summary>
		public async Task<MachineOperationResult> DeleteFile(string filePath)
		{
			try
			{
				await SendCommand(ELEGOOCmd.DeleteFile, new
				{
					FileList = new[] { filePath },
					FolderList = Array.Empty<string>()
				});

				await RefreshFileList();

				Log.Info($"Deleted file: {filePath}");
				return MachineOperationResult.Ok;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to delete file: {filePath}");
				return MachineOperationResult.Fail("elegoo.delete.file.failed", "Delete File Failed", ex.Message);
			}
		}

		/// <summary>
		/// Enables or disables the printer's video stream.
		/// </summary>
		public async Task<MachineOperationResult> EnableVideoStream(bool enable)
		{
			try
			{
				await SendCommand(ELEGOOCmd.EditVideoStreaming, new { Enable = enable ? 1 : 0 });
				Log.Info($"Video stream {(enable ? "enabled" : "disabled")}");
				return MachineOperationResult.Ok;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to toggle video stream");
				return MachineOperationResult.Fail("elegoo.video.stream.failed", "Video Stream Toggle Failed", ex.Message);
			}
		}

		/// <summary>
		/// Requests the printer's file list. Results arrive asynchronously via <see cref="HandleFileListResponse"/>.
		/// </summary>
		public async Task RefreshFileList()
		{
			await SendCommand(ELEGOOCmd.GetFileList, new { Url = "/local" });
		}

		/// <summary>
		/// Requests the printer's print history IDs. Detail retrieval follows automatically.
		/// </summary>
		public async Task RefreshHistory()
		{
			await SendCommand(ELEGOOCmd.GetHistoryIds);
		}

		#endregion

		/// <summary>
		/// Host/IP only for <c>ws://…:3030</c> / <c>http://…:3030</c>. Strips pasted <c>http(s)://</c>, <c>ws(s)://</c>, paths, and a trailing <c>:3030</c>.
		/// </summary>
		private static string NormalizePrinterHost(string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return raw ?? string.Empty;

			var s = raw.Trim();

			if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
				s = s["http://".Length..];
			else if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				s = s["https://".Length..];
			else if (s.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
				s = s["ws://".Length..];
			else if (s.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
				s = s["wss://".Length..];

			var pathSlash = s.IndexOf('/');
			if (pathSlash >= 0)
				s = s[..pathSlash];

			s = s.Trim();

			if (s.EndsWith(":3030", StringComparison.Ordinal))
				s = s[..^":3030".Length];

			return s.Trim();
		}

		#region DownloadLocalFile (Not Supported)

		protected override Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream)
		{
			throw new NotSupportedException(
				"ELEGOO Centauri printers do not expose a file download endpoint. " +
				"Files can only be uploaded and managed, not downloaded back.");
		}

		#endregion
	}
}
