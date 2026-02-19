using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.Extensions;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using MQTTnet;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lib3Dp.Constants;

namespace Lib3Dp.Connectors.BambuLab.MQTT
{
	internal partial class BBLMQTTConnection
	{
		// https://github.com/bambulab/BambuStudio/blob/07323b69d83d461aecb9a42b1f13363dab2f5e5a/src/slic3r/GUI/DeviceCore/DevFilaSystem.cpp

		private readonly Logger Logger;

		private readonly IMqttClient MQTT = new MqttClientFactory().CreateMqttClient();

		private readonly Dictionary<int, string> AMSIDToSN = [];
		private readonly Dictionary<string, int> SNToAMSID = [];

		private CancellationTokenSource ContinuouslyReconnectTokenSource;
		private PeriodicAsyncAction? PullAllChangesPeriodic;

		public BBLMQTTSettings Settings { get; private set; }

		public bool IsConnected => MQTT.IsConnected;

		public event Action<BBLMQTTData>? OnData;

		public BBLMQTTConnection(Logger loggerToUse)
		{
			Logger = loggerToUse;
			ContinuouslyReconnectTokenSource = new();

			MQTT.ConnectedAsync += OnConnected;
			MQTT.DisconnectedAsync += OnDisconnected;
			MQTT.ApplicationMessageReceivedAsync += OnMessage;
		}

		public async Task AutoConnectAsync(BBLMQTTSettings settings)
		{
			if (MQTT.IsConnected) return;

			this.ContinuouslyReconnectTokenSource = new CancellationTokenSource();
			this.Settings = settings;

			await MQTT.ConnectAsync(BuildMQTTOptions(settings), this.ContinuouslyReconnectTokenSource.Token);
			await MQTT.PingAsync(this.ContinuouslyReconnectTokenSource.Token);

			Logger.Info("Connected via MQTT");
		}

		public async Task DisconnectAsync()
		{
			if (!MQTT.IsConnected) return;

			await this.MQTT.DisconnectAsync();

			this.ContinuouslyReconnectTokenSource.Cancel();
			this.AMSIDToSN.Clear();
			this.SNToAMSID.Clear();
		}

		private async Task PublishCommand(string category, string command, JsonObject? commandData = null)
		{
			if (!IsConnected) return;
			commandData ??= new JsonObject();
			commandData.Add("command", command);
			commandData.Add("sequence_id", "0");
			commandData.Add("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds());

			var data = new JsonObject
			{
				{ category, commandData }
			};
			await MQTT.PublishStringAsync(BBLConstants.MQTT.RequestTopic(this.Settings.SerialNumber), data.ToJsonString());
		}

		public async Task PublishPrint(BBLPrintOptions options)
		{
			// Verified for P2S

			// NOTICE: For some reason job_id makes the local file on either the sdcard or usb drive to be removed.
			// DO NOT USE JOB_ID
			// DO NOT USE TASK_ID, YOUR INTERFACE WILL BREAK.

			// For additional metadata subtask_id seems to be able to hold them.

			// TODO: There may be an issue with a long subtask_id, I've noticed some prints take 30 seconds to begin and lockup the whole system. More testing is required.

			var commandData = new JsonObject
			{
				{ "auto_bed_leveling", 2 },
				{ "bed_leveling", options.BedLeveling },
				{ "cfg", "1" },
				{ "extrude_cali_flag", 2 },
				{ "flow_cali", true },
				{ "nozzle_mapping", new JsonArray() },
				{ "nozzle_offset_cali", 0 },
				{ "param", $"Metadata/plate_{options.PlateIndex}.gcode" },
				{ "plate", options.PlateIndex },
				{ "subtask_name", "" },
				{ "task_type", 1 },
				{ "subtask_id", options.MetadataId },
				{ "timelapse", options.Timelapse },
				{ "toolhead_offset_cali", false },
				{ "url", $"file:///media/usb0/{options.FileName}" }
			};

			if (options.AMSMapping != null && options.AMSMapping.Count > 0)
			{
				var amsMapping2 = new JsonArray();

				for (int i = 1; i <= options.ProjectFilamentCount; i++)
				{
					if (options.AMSMapping.TryGetValue(i, out var slot))
					{
						amsMapping2.Add(new JsonObject
						{
							["ams_id"] = slot.AMSId,
							["slot_id"] = slot.SlotId
						});
					}
					else
					{
						// Project material ID not used by the specified plate should still be included, but marked as 255, 255 on both the AMS and Slot.

						amsMapping2.Add(new JsonObject
						{
							["ams_id"] = 255,
							["slot_id"] = 255
						});
					}
				}

				commandData.Add("ams_mapping2", amsMapping2);
				commandData.Add("use_ams", true);
			}
			else
			{
				commandData.Add("use_ams", false);
			}

			await PublishCommand("print", "project_file", commandData);
		}

		public async Task PublishClearBed()
		{
			await PublishCommand("print", "bed_clean");
		}

		//public async Task PublishTraySetting(BBLFilament filament)
		//{
		//    var commandData = new JsonObject
		//    {
		//        { "ams_id", filament.Location.IsAutomatic ? (int)Math.Floor(filament.Location.Slot / 4d) : 255 },
		//        { "nozzle_temp_max", filament.NozzleTempMax },
		//        { "nozzle_temp_min", filament.NozzleTempMin },
		//        { "setting_id", filament.SettingID },
		//        { "slot_id", filament.Location.Slot % 4 },
		//        { "tray_color", $"{filament.Base.Color.Hex}FF" },
		//        { "tray_id", filament.TrayID },
		//        { "tray_info_idx", filament.TrayInfoIDX },
		//        { "tray_type", filament.Base.Material }
		//    };
		//    await this.PublishCommand("print", "ams_filament_setting", commandData);
		//}

		public async Task PublishClearError(int errorCode)
		{
			var commandData = new JsonObject
			{
				{ "print_error", errorCode }
			};
			await PublishCommand("print", "clean_print_error", commandData);
		}

		public async Task PublishStop()
		{
			await PublishCommand("print", "stop");
		}

		public async Task PublishPause()
		{
			await PublishCommand("print", "pause");
		}

		public async Task PublishResume()
		{
			await PublishCommand("print", "resume");
		}

		public async Task PublishLEDControl(string fixtureName, bool isOn)
		{
			var commandData = new JsonObject
			{
				{ "led_mode", isOn ? "on" : "off" },
				{ "led_node", fixtureName },
				{ "interval_time", 500 },
				{ "led_off_time", 250 },
				{ "led_on_time", 250 },
				{ "loop_times", 30 }
			};

			await PublishCommand("system", "ledctrl", commandData);
		}

		public Task PublishAMSHeatingCommand(string amsSN, HeatingSettings settings)
		{
			// https://github.com/greghesp/ha-bambulab/issues/1448
			var commandData = new JsonObject
			{
				{ "duration", settings.Duration.TotalHours },
				{ "humidity", 0 },
				{ "ams_id", SNToAMSID[amsSN] },
				{ "mode", 1 },
				{ "rotate_tray", settings.DoSpin },
				{ "temp", settings.TempC },
				{ "cooling_temp", 40 } // Unsure what this field does. 

            };
			return PublishCommand("print", "ams_filament_drying", commandData);
		}

		public Task PublishAMSStopHeatingCommand(string amsSN)
		{
			// https://github.com/greghesp/ha-bambulab/issues/1448
			var commandData = new JsonObject
			{
				{ "duration", 0 },
				{ "humidity", 0 },
				{ "ams_id", SNToAMSID[amsSN] },
				{ "mode", 0 },
				{ "rotate_tray", false },
				{ "temp", 0 },
				{ "cooling_temp", 40 } // Unsure what this field does. 

            };
			return PublishCommand("print", "ams_filament_drying", commandData);
		}

		public Task PublishHVACModeCommand(MachineAirDuctMode mode)
		{
			var commandData = new JsonObject
			{
				{ "modeId", mode == MachineAirDuctMode.Heating ? 1 : 0 }
			};
			return PublishCommand("print", "set_airduct", commandData);
		}

		public async Task PublishGetFirmwareVersion()
		{
			await PublishCommand("info", "get_version");
		}

		public async Task PublishPushAll()
		{
			await PublishCommand("pushing", "pushall");
		}

		private async Task OnConnected(MqttClientConnectedEventArgs ev)
		{
			await MQTT.SubscribeAsync(BBLConstants.MQTT.ReportTopic(this.Settings.SerialNumber));

			// Mark as connected once state is known by MQTT data.

			await PublishGetFirmwareVersion();
			await PublishPushAll();

			PullAllChangesPeriodic ??= new PeriodicAsyncAction(TimeSpan.FromMinutes(15), PublishPushAll);
		}

		private async Task OnDisconnected(MqttClientDisconnectedEventArgs ev)
		{
			OnData?.Invoke(new BBLMQTTData
			{
				Changes = new MachineStateUpdate().SetStatus(MachineStatus.Disconnected) 
			});

			// TODO: Read ev.ConnectResults.ResultCode and push to notifications.

			if (PullAllChangesPeriodic != null)
			{
				await PullAllChangesPeriodic.DisposeAsync();
				PullAllChangesPeriodic = null;
			}

			_ = ContinuouslyReconnect();
		}

		private Task ContinuouslyReconnect()
		{
			return Task.Run(async () =>
			{
				while (!MQTT.IsConnected && !ContinuouslyReconnectTokenSource.Token.IsCancellationRequested)
				{
					Thread.Sleep(TimeSpan.FromSeconds(10));
					try
					{
						await MQTT.ReconnectAsync();
					}
					catch (Exception)
					{
						;
					}
				}
			});
		}

		private async Task OnMessage(MqttApplicationMessageReceivedEventArgs ev)
		{
			JsonDocument JSON;
			try
			{
				JSON = JsonDocument.Parse(ev.ApplicationMessage.Payload);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to parse MQTT JSON payload.");
				return;
			}

			BBLMQTTData readData;
			try
			{
				readData = new BBLMQTTData(new MachineStateUpdate(), null, null, null, null);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Failed to initialize BBLMQTTData.");
				return;
			}

			if (JSON.RootElement.TryGetProperty("print", out var printJSON))
			{
				TryRun(() => OnMessagePrintMQTTSecurity(printJSON, ref readData), "Print.Security");
				TryRun(() => OnMessagePrintSDCard(printJSON, ref readData), "Print.SDCard");
				TryRun(() => OnMessagePrintGcodeState(printJSON, ref readData), "Print.GcodeState");
				TryRun(() => OnMessagePrintJob(printJSON, ref readData), "Print.Job");

				OnMessageTemps(printJSON, ref readData);

				if (printJSON.TryGetProperty("device", out var devicesJSON))
					TryRun(() => OnMessagePrintDevices(devicesJSON, ref readData), "Print.Devices");

				TryRun(() => OnMessagePrintMaterials(printJSON, ref readData), "Print.Materials");
				TryRun(() => OnMessagePrintNozzles(printJSON, ref readData), "Print.Nozzles");
				TryRun(() => OnMessagePrintLighting(printJSON, ref readData), "Print.Lighting");
			}

			if (JSON.RootElement.TryGetProperty("info", out var infoJSON))
				TryRun(() => OnMessageInfoVersion(infoJSON, ref readData), "Info.Version");

			if (readData.UpdateAMSMapping == true)
			{
				try
				{
					await PublishGetFirmwareVersion();
					Logger.Trace("AMS ID -> AMS SN mappings requested.");
				}
				catch (Exception ex)
				{
					Logger.Error(ex, "Failed to publish firmware version request.");
				}
			}

			try
			{
				OnData?.Invoke(readData);
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "OnData handler threw an exception.");
			}

		}

		private void TryRun(Action action, string context)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, $"Exception while handling MQTT message section: {context}");
				throw;
			}
		}

		private static void OnMessagePrintDevices(JsonElement devicesJSON, ref BBLMQTTData data)
		{
			if (devicesJSON.TryGetPropertyChain(out var modeCurElem, "airduct", "modeCur"))
				data.Changes.SetAirDuctMode(modeCurElem.GetInt32() == 1 ? MachineAirDuctMode.Heating : MachineAirDuctMode.Cooling);
		}

		private static void OnMessagePrintMQTTSecurity(JsonElement printJSON, ref BBLMQTTData data)
		{
			// TODO: NOT WORKING

			bool isResultFailed = printJSON.TryGetProperty("result", out var resultElem) && resultElem.GetString()!.Equals("failed", StringComparison.OrdinalIgnoreCase);
			bool isReasonMsgSecurityFailed = printJSON.TryGetProperty("reason", out var failedReasonElem) && failedReasonElem.GetString()!.Equals(BBLConstants.MQTT.SECURITY_FAILED_ERROR_MSG, StringComparison.OrdinalIgnoreCase);

			if (isResultFailed && isReasonMsgSecurityFailed)
				// mqtt message verify failed indicates we do not have permission to control this machine.
				data.UsesUnsupportedSecurity = true;
		}

		private void OnMessagePrintGcodeState(JsonElement printJSON, ref BBLMQTTData data)
		{
			if (printJSON.TryGetString(out var gcode_state, "gcode_state"))
			{
				data.Changes.SetStatus(gcode_state.ToLower() switch
				{
					"idle" => MachineStatus.Idle,
					"running" => MachineStatus.Printing,
					"pause" => MachineStatus.Paused,
					"finish" => MachineStatus.Printed,
					"failed" => MachineStatus.Canceled,
					_ => throw new Exception($"Unknown gcode_state value {gcode_state.ToLower()}")
				});
			}

			if (printJSON.TryGetInt32(out var print_error, "print_error"))
			{
				if (print_error != 0 && data.Changes.Status == MachineStatus.Printing)
				{
					// Sometimes machine reports running when print_error is present..?
					data.Changes.SetStatus(MachineStatus.Paused);
				}
			}
		}

		private void OnMessagePrintJob(JsonElement printJSON, ref BBLMQTTData data)
		{
			if (printJSON.TryGetInt32(out var mc_percent, "mc_percent"))
				data.Changes.UpdateCurrentJob(changes => changes.SetPercentageComplete(mc_percent));

			if (printJSON.TryGetInt32(out var stg_cur, "stg_cur"))
			{
				//Logger.Trace($"{nameof(stg_cur)}: {stg_cur}");

				data.Changes.UpdateCurrentJob(changes => changes.SetSubStage(stg_cur switch
				{
					BBLConstants.PrintStages.COOLING_CHAMBER => "Cooling Chamber",
					BBLConstants.PrintStages.IDENTIFYING_BUILD_PLATE => "Identifying Build Plate",
					BBLConstants.PrintStages.HOMING_TOOLHEAD_STAGE => "Homing Toolhead",
					BBLConstants.PrintStages.CHANGING_FILAMENT => "Changing Filament",
					_ => null
				}));
			}

			if (printJSON.TryGetString(out var subtask_name, "subtask_name"))
			{
				var processedName = string.IsNullOrWhiteSpace(subtask_name) ? "None" : subtask_name;

				data.Changes.UpdateCurrentJob(changes => changes.SetName(processedName));
			}

			if (printJSON.TryGetInt32(out var mc_remaining_time, "mc_remaining_time"))
			{
				var remainingTimeSpan = TimeSpan.FromSeconds(mc_remaining_time);

				data.Changes.UpdateCurrentJob(changes => changes.SetRemainingTime(remainingTimeSpan));
			}

			if (printJSON.TryGetString(out var metadata_id, "subtask_id")
				&& PrefixedFixedLengthKeyValueMessage.TryParse(metadata_id, "Lib3Dp", out var metadata))
			{
				var encodedTotalTime = TimeSpan.FromMinutes(int.Parse(metadata.Values["Minutes"]));
				var encodedPath = metadata.Values["Path"];
				var encodedHash = metadata.Values["Hash"];
				var encodedGramsUsed = int.Parse(metadata.Values["Grams"]);

				data.Changes.UpdateCurrentJob(changes => changes
					.SetTotalTime(encodedTotalTime)
					.SetLocalPath(encodedPath)
					.SetTotalMaterialUsage(encodedGramsUsed)
					.SetFile(BBLFiles.HandleAs3MF(this.Settings.SerialNumber, encodedPath, encodedHash))); // TODO: These modules should have access to BBLMachineConnection. ID is inferred as SN.

				if (metadata.Values.TryGetValue("ThumbnailSmallHash", out var encodedThumbnailSmallHash))
				{
					data.Changes.UpdateCurrentJob(changes => changes.SetThumbnail(BBLFiles.HandleAs3MFThumbnail(this.Settings.SerialNumber, encodedPath, encodedThumbnailSmallHash)));
				}
			}

			// Issue: Cannot calculate if machine has already finished its print.

			if (data.Changes.CurrentJob != null
				&& data.Changes.CurrentJob.PercentageCompleteIsSet
				&& data.Changes.CurrentJob.RemainingTimeIsSet
				&& TryCalculateTotalTime(data.Changes.CurrentJob!.RemainingTime.TotalSeconds, data.Changes.CurrentJob.PercentageComplete, out var calculatedTotalTime))
			{
				data.Changes.UpdateCurrentJob(changes => changes.SetTotalTime(calculatedTotalTime.Value));
			}
		}

		private void OnMessagePrintMaterials(JsonElement printJSON, ref BBLMQTTData data)
		{
			// TODO: Extended support for AMS Drying.
			// BBL will be adding the "dry_setting" object soon which gives the target temp, filament, and total duration.
			// https://github.com/bambulab/BambuStudio/blob/07323b69d83d461aecb9a42b1f13363dab2f5e5a/src/slic3r/GUI/DeviceCore/DevFilaSystem.cpp#L507
			// This functionality is only out for the H2D (As of 1/27/2026)
			//DevJsonValParser::ParseVal(j_ams, "dry_time", curr_ams->m_left_dry_time);
			//if (obj->is_support_remote_dry)
			//{
			//    if (j_ams.contains("info"))
			//    {
			//        const std::string&info = j_ams["info"].get < std::string> ();
			//        curr_ams->m_dry_status = (DevAms::DryStatus)DevUtil::get_flag_bits(info, 4, 4);
			//        curr_ams->m_dry_fan1_status = (DevAms::DryFanStatus)DevUtil::get_flag_bits(info, 18, 2);
			//        curr_ams->m_dry_fan2_status = (DevAms::DryFanStatus)DevUtil::get_flag_bits(info, 20, 2);
			//        curr_ams->m_dry_sub_status = (DevAms::DrySubStatus)DevUtil::get_flag_bits(info, 22, 4);
			//    }

			//    if (j_ams.contains("dry_setting"))
			//    {
			//        const auto&j_dry_settings = j_ams["dry_setting"];
			//        DevAms::DrySettings dry_settings;
			//        DevJsonValParser::ParseVal(j_dry_settings, "dry_filament", dry_settings.dry_filament);
			//        DevJsonValParser::ParseVal(j_dry_settings, "dry_temperature", dry_settings.dry_temp);
			//        DevJsonValParser::ParseVal(j_dry_settings, "dry_duration", dry_settings.dry_hour);
			//        curr_ams->m_dry_settings = dry_settings;
			//    }

			//    if (j_ams.contains("dry_sf_reason"))
			//    {
			//        curr_ams->m_dry_cannot_reasons = DevJsonValParser::GetVal<std::vector<DevAms::CannotDryReason>>(j_ams, "dry_sf_reason");
			//    }
			//}

			//{
			//	if ((printJSON.TryGetProperty("vt_tray", out var vt_tray) || printJSON.TryGetProperty("vir", out vt_tray)) && vt_tray.TryGetString(out var vt_tray_id, "id"))
			//	{
			//		ParseTrayAndAMSInt32(ushort.Parse(vt_tray_id), out var amsN, out var trayN);

			//		data.Changes.UpdateMaterialUnits(amsN.ToString(), amsConfigure => amsConfigure
			//			.SetCapabilities(MaterialUnitCapabilities.None)
			//			.SetCapacity(1));

			//		if (vt_tray.TryGetString(out var tray_type, "tray_type"))
			//			//data.Changes.UpdateMaterialUnits("", what => what.UpdateLoaded(0, duh => duh.))
			//			data.Changes.UpdateMaterialUnits(amsN.ToString(), ams => ams.UpdateTrays(trayN, tray => tray.UpdateMaterial(material => material.SetName(tray_type))));

			//		if (vt_tray.TryGetString(out var tray_color, "tray_color"))
			//			data.Changes.UpdateMaterialUnits(amsN.ToString(), ams => ams.UpdateTrays(trayN, tray => tray.UpdateMaterial(material => material.SetColor(new MaterialColor(null, tray_color)))));

			//		if (vt_tray.TryGetString(out var tray_info_idx, "tray_info_idx"))
			//			data.Changes.UpdateMaterialUnits(amsN.ToString(), ams => ams.UpdateTrays(trayN, tray => tray.UpdateMaterial(material => material.SetFProfileIDX(tray_info_idx))));
			//	}
			//}

			if (printJSON.TryGetPropertyChain(out var amsArrayElem, "ams", "ams"))
			{
				var amsCount = amsArrayElem.GetArrayLength();

				foreach (var amsElem in amsArrayElem.EnumerateArray())
				{
					if (!amsElem.TryGetString(out var amsId, "id")) continue;

					if (!AMSIDToSN.TryGetValue(int.Parse(amsId), out var amsSN))
					{
						// Missing mapping, request get_version.
						data.UpdateAMSMapping = true;

						Logger.Warning($"Missing AMS ID to SN Mapping for AMS #{amsId}");

						continue;
					}

					var amsModel = BBLConstants.GetAMSModelFromSN(amsSN);

					data.Changes.UpdateMaterialUnits(amsSN, amsConfigure => amsConfigure
						.SetCapacity(BBLConstants.GetAMSCapacityFromModel(amsModel))
						.SetModel(amsModel)
						.SetCapabilities(BBLConstants.GetAMSFeaturesFromModel(amsModel))
						.SetHeatingConstraints(BBLConstants.GetAMSHeatingConstraintsFromModel(amsModel)));

					// Loaded Materials

					if (amsElem.TryGetProperty("tray", out var traysElem))
					{
						foreach (var trayElem in traysElem.EnumerateArray())
						{
							if (!trayElem.TryGetString(out var trayId_s, "id") || !int.TryParse(trayId_s, out var trayId)) continue;

							data.Changes.UpdateMaterialUnits(amsSN, ams => ams.UpdateTrays(trayId, tray => tray.SetGramsMaximum(1000)));

							if (trayElem.TryGetString(out var tray_type, "tray_type"))
								data.Changes.UpdateMaterialUnits(amsSN, ams => ams.UpdateTrays(trayId, tray => tray.UpdateMaterial(material => material.SetName(tray_type))));

							if (trayElem.TryGetString(out var tray_color, "tray_color"))
								data.Changes.UpdateMaterialUnits(amsSN, ams => ams.UpdateTrays(trayId, tray => tray.UpdateMaterial(material => material.SetColor(new MaterialColor(null, tray_color)))));

							if (trayElem.TryGetString(out var tray_info_idx, "tray_info_idx"))
								data.Changes.UpdateMaterialUnits(amsSN, ams => ams.UpdateTrays(trayId, tray => tray.UpdateMaterial(material => material.SetFProfileIDX(tray_info_idx))));
						}
					}

					{
						if (amsElem.TryGetString(out var temp_C_s, "temp") && double.TryParse(temp_C_s, out var temp_C))
							data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetTemperatureC(temp_C));

						if (amsElem.TryGetString(out var humidity_percent_S, "humidity_raw") && int.TryParse(humidity_percent_S, out var humidity_percent))
							data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetHumidityPercent(humidity_percent));
					}

					if (amsElem.TryGetInt32(out var dry_time, "dry_time"))
					{
						if (dry_time > 0 && amsElem.TryGetString(out var temp_C_s, "temp") && double.TryParse(temp_C_s, out var temp_C))
						{
							var activeHeatingSettings = new HeatingJob(temp_C, TimeSpan.FromMinutes(dry_time));

							data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetHeatingJob(activeHeatingSettings));
						}
						else if (dry_time <= 0)
						{
							data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetHeatingJob(null));
						}

						//if (dry_time > 0 && amsElem.TryGetInt32(out var dry_temperature, "dry_setting", "dry_temperature"))
						//{
						//	var activeHeatingSettings = new HeatingJob(dry_temperature, TimeSpan.FromMinutes(dry_time));

						//	data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetHeatingJob(activeHeatingSettings));
						//}
						//else if (dry_time <= 0)
						//{
						//	data.Changes.UpdateMaterialUnits(amsSN, configure => configure.SetHeatingJob(null));
						//}
					}
				}
			}
		}

		private void OnMessagePrintFunctionality(JsonElement printJSON, ref BBLMQTTData data)
		{
			// TODO: fun and fun2 express the capabilities on the printer.
			// https://github.com/bambulab/BambuStudio/blob/07323b69d83d461aecb9a42b1f13363dab2f5e5a/src/slic3r/GUI/DeviceManager.cpp#L4105


		}

		private void OnMessagePrintNozzles(JsonElement printJSON, ref BBLMQTTData data)
		{
			// Basic Support (legacy single nozzle/extruder)
			ProcessLegacyNozzleData(printJSON, ref data);

			// Advanced Support (multi-nozzle/extruder)
			ProcessAdvancedNozzleData(printJSON, ref data);
		}

		private void ProcessLegacyNozzleData(JsonElement printJSON, ref BBLMQTTData data)
		{
			if (printJSON.TryGetString(out var nozzle_diameter_s, "nozzle_diameter")
				&& double.TryParse(nozzle_diameter_s, out var nozzle_diameter))
			{
				data.Changes.UpdateNozzles(0, nozzle => nozzle.SetDiameter(nozzle_diameter));
			}

			if (printJSON.TryGetDouble(out var nozzle_temper, "nozzle_temper"))
			{
				data.Changes.UpdateExtruders(0, extruder => extruder.SetTempC(nozzle_temper));
			}

			if (printJSON.TryGetDouble(out var nozzle_target_temper, "nozzle_target_temper"))
			{
				data.Changes.UpdateExtruders(0, extruder => extruder.SetTargetTempC(nozzle_target_temper));
			}

			//if (printJSON.TryGetString(out var tray_now_s, "ams", "tray_now")
			//	&& ushort.TryParse(tray_now_s, out var tray_now))
			//{
			//	UpdateExtruderSpool(0, tray_now, ref data);
			//}
		}

		private void ProcessAdvancedNozzleData(JsonElement printJSON, ref BBLMQTTData data)
		{
			if (printJSON.TryGetPropertyChain(out var nozzles, "device", "nozzle", "info"))
			{
				foreach (var nozzle in nozzles.EnumerateArray())
				{
					if (nozzle.TryGetInt32(out int number, "id")
						&& nozzle.TryGetDouble(out double diameter, "diameter"))
					{
						data.Changes.UpdateNozzles(number, config => config.SetDiameter(diameter));
					}
				}
			}

			if (printJSON.TryGetPropertyChain(out var extruders, "device", "extruder", "info"))
			{
				foreach (var extruder in extruders.EnumerateArray())
				{
					if (!extruder.TryGetInt32(out int number, "id")) continue;

					ProcessExtruderElement(extruder, number, ref data);
				}
			}
		}

		private void ProcessExtruderElement(JsonElement extruder, int number, ref BBLMQTTData data)
		{
			var heatingConstraints = BBLConstants.GetHeatingConstraintsFromElementName(HeatingElementNames.Nozzle, this.Settings.Model);
			if (heatingConstraints.HasValue)
			{
				data.Changes.UpdateExtruders(number, e => e.SetHeatingConstraint(heatingConstraints.Value));
			}

			if (extruder.TryGetInt32(out var temp, "temp"))
			{
				var current_temp = BitUtils.GetBitsFromNumb(temp, 0, 16);
				var target_temp = BitUtils.GetBitsFromNumb(temp, 16, 16);
				data.Changes.UpdateExtruders(number, e => e.SetTempC(current_temp).SetTargetTempC(target_temp));
			}

			if (extruder.TryGetInt32(out var snow, "snow"))
			{
				UpdateExtruderSpool(number, (ushort)snow, ref data);
			}

			if (extruder.TryGetInt32(out var hotend_id_used_now, "hnow"))
			{
				data.Changes.UpdateExtruders(number, e => e.SetNozzleNumber(hotend_id_used_now));
			}
		}

		private void UpdateExtruderSpool(int extruderNumber, ushort trayId, ref BBLMQTTData data)
		{
			if (trayId is BBLConstants.NotLoadedID or 65535)
			{
				data.Changes.UpdateExtruders(extruderNumber, e => e.RemoveLoadedSpool());
			}
			else if (trayId == BBLConstants.ExternalTrayID)
			{
				data.Changes.UpdateExtruders(extruderNumber, e => e
					.SetLoadedSpool(new SpoolLocation(BBLConstants.ExternalTrayMMID, BBLConstants.ExternalTraySlotNumber)));
			}
			else
			{
				ParseTrayAndAMSInt32(trayId, out var amsID, out var trayN);
				if (AMSIDToSN.TryGetValue(amsID, out var amsSN))
				{
					data.Changes.UpdateExtruders(extruderNumber, e => e.SetLoadedSpool(new SpoolLocation(amsSN, trayN)));
				}
				else
				{
					data.UpdateAMSMapping = true;
				}
			}
		}

		private void OnMessagePrintHMS(JsonElement printJSON, ref BBLMQTTData data)
		{
			// Sample
			// print.hms
			// "hms": [
			// {
			//     "attr": 83886848,
			//    "code": 131086
			// }
			// ],
		}

		private void OnMessageTemps(JsonElement printJSON, ref BBLMQTTData data)
		{
			// TODO: Partialize, support for multi nozzle.

			if (printJSON.TryGetDouble(out var bed_target_temper, "bed_target_temper") && printJSON.TryGetDouble(out var bed_temper, "bed_temper"))
			{
				var constraints = BBLConstants.GetHeatingConstraintsFromElementName(HeatingElementNames.Bed, this.Settings.Model);

				if (constraints.HasValue)
					data.Changes.SetHeatingElements(HeatingElementNames.Bed, new HeatingElement(Math.Round(bed_temper), Math.Round(bed_target_temper), constraints.Value));
			}

			if (printJSON.TryGetDouble(out var nozzle_target_temper, "nozzle_target_temper") && printJSON.TryGetDouble(out var nozzle_temper, "nozzle_temper"))
			{
				var constraints = BBLConstants.GetHeatingConstraintsFromElementName(HeatingElementNames.Nozzle, this.Settings.Model);

				if (constraints.HasValue)
					data.Changes.SetHeatingElements(HeatingElementNames.Nozzle, new HeatingElement(Math.Round(nozzle_temper), Math.Round(nozzle_target_temper), constraints.Value));
			}

			// TODO: Verify chamber_target_temper exists on X1E
			if (printJSON.TryGetDouble(out var chamber_target_temper, "chamber_target_temper") && printJSON.TryGetDouble(out var chamber_temper, "chamber_temper"))
			{
				var constraints = BBLConstants.GetHeatingConstraintsFromElementName(HeatingElementNames.Chamber, this.Settings.Model);

				if (constraints.HasValue)
					data.Changes.SetHeatingElements(HeatingElementNames.Chamber, new HeatingElement(Math.Round(chamber_temper), Math.Round(chamber_target_temper), constraints.Value));
			}
		}

		private static void OnMessagePrintSDCard(JsonElement printJSON, ref BBLMQTTData data)
		{
			if (printJSON.TryGetBoolean(out bool sdcard, "sdcard"))
				data.HasUSBOrSDCard = sdcard;
		}

		private static void OnMessagePrintLighting(JsonElement printJSON, ref BBLMQTTData data)
		{
			//"lights_report": [
			//  {
			//    "mode": "on",
			//    "node": "chamber_light"
			//  },
			//  {
			//    "mode": "flashing",
			//    "node": "work_light"
			//  }
			//],

			if (printJSON.TryGetProperty("lights_report", out var lights_report))
			{
				foreach (var light_fixture in lights_report.EnumerateArray())
				{

					if (light_fixture.TryGetString(out string name, "node")
						&& light_fixture.TryGetString(out string mode, "mode"))
					{

						var isOn = !mode.Equals("off");

						if (name.Equals("chamber_light"))
							data.Changes.SetLights("Chamber", isOn);
					}


				}
			}

		}

		public static void ParseTrayAndAMSInt32(ushort value, out ushort ams, out ushort tray)
		{
			tray = (ushort)BitUtils.GetBitsFromNumb(value, 0, 8);
			ams = (ushort)BitUtils.GetBitsFromNumb(value, 8, 8);

			//uint u = value;
			//tray = (ushort)(u & 0xFF); // bits 0..7
			//ams = (ushort)(u >> 8 & 0xFF); // bits 8..15
		}

		/// <summary>
		/// Tries to get the AMS ID (integer) from the AMS serial number.
		/// </summary>
		public bool TryGetAMSIdFromSN(string amsSN, out int amsId)
		{
			return SNToAMSID.TryGetValue(amsSN, out amsId);
		}

		private void OnMessageInfoVersion(JsonElement infoJSON, ref BBLMQTTData data)
		{
			if (!infoJSON.TryGetString(out var command, "command") ||
				!command.Equals("get_version", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			if (!infoJSON.TryGetProperty("module", out var moduleProperty))
				return;

			foreach (var moduleJSON in moduleProperty.EnumerateArray())
			{
				var module = moduleJSON.Deserialize<BBLModule>();

				if (module.InternalName.Equals("ota", StringComparison.OrdinalIgnoreCase))
				{
					data.FirmwareVersion = module.Version;
					Logger.Trace($"Machine Firmware Version {data.FirmwareVersion}");
				}
				else
				{
					TryParseAMSModule(module);
				}
			}
		}

		private void TryParseAMSModule(BBLModule module)
		{
			var amsPrefixes = new[]
			{
				("n3f/", 4),      // AMS 2 Pro
                ("ams_f1/", 7),   // AMS Lite
                ("n3s/", 4),      // AMS HT
                ("ams/", 4)       // AMS
            };

			foreach (var (prefix, length) in amsPrefixes)
			{
				if (module.InternalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				{
					if (int.TryParse(module.InternalName.AsSpan(length), out int amsId))
					{
						AMSIDToSN[amsId] = module.SN;
						SNToAMSID[module.SN] = amsId;
					}
					break;
				}
			}
		}

		public static bool TryCalculateTotalTime(double secondsRemaining, double percentCompleted, [NotNullWhen(true)] out TimeSpan? totalTime)
		{
			totalTime = null;

			if (percentCompleted >= 100) return false;

			if (percentCompleted < 0) return false;

			double percentRemaining = (100.0 - percentCompleted) / 100.0;

			if (percentRemaining <= 0) return false;

			totalTime = TimeSpan.FromSeconds((long)(secondsRemaining / percentRemaining));

			return true;
		}

		private static MqttClientOptions BuildMQTTOptions(BBLMQTTSettings settings)
		{
			return new MqttClientOptionsBuilder()
				.WithTcpServer(settings.Address, 8883)
				.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
				.WithClientId("Lib3Dp")
				.WithCleanSession(true)
				.WithProtocolType(System.Net.Sockets.ProtocolType.Tcp)
				.WithCredentials("BBLP".ToLower(), settings.AccessCode)
				.WithTimeout(TimeSpan.FromSeconds(15))
				.WithTlsOptions(new MqttClientTlsOptions
				{
					UseTls = true,
					AllowUntrustedCertificates = true,
					AllowRenegotiation = true,
					IgnoreCertificateChainErrors = true,
					CertificateValidationHandler = (cV) =>
					{
						return true;
					}
				})
				.Build();
		}
	}
}
