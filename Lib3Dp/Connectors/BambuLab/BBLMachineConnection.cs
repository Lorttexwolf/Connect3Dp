using FluentFTP.Exceptions;
using Lib3Dp.Cameras;
using Lib3Dp.Configuration;
using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.Connectors.BambuLab.FTP;
using Lib3Dp.Connectors.BambuLab.MQTT;
using Lib3Dp.Exceptions;
using Lib3Dp.Extensions;
using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using MQTTnet.Exceptions;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace Lib3Dp.Connectors.BambuLab
{
	public class BBLMachineConnection : MachineConnection, IConfigurableConnection<BBLMachineConnection, BBLMachineConfiguration>
	{
		public BBLMachineConfiguration Configuration { get; private set; }
		public string PrefixSerialNumber => Configuration.SerialNumber[..3];
		public string Model => BBLConstants.GetModelFromSerialNumber(Configuration.SerialNumber);

		private readonly BBLFTPConnection FTP;
		private readonly BBLMQTTConnection MQTT;

		private BBLFirmwareVersion? FirmwareVersion;
		private bool PrevUsesUnsupportedSecurity;
		private bool HasUSBOrSDCard;
		private readonly bool IsUSBOrSDCardRequired;

		private BBLMachineConnection(IMachineFileStore fileStore, BBLMachineConfiguration configuration) 
			: base(fileStore, new MachineConnectionConfiguration(configuration.Nickname, $"BLL{configuration.SerialNumber}", "Bambu Lab", BBLConstants.GetModelFromSerialNumber(configuration.SerialNumber)))
		{
			this.Configuration = configuration;
			this.MQTT = new BBLMQTTConnection(Logger.OfCategory($"BBL MQTT {Configuration.Nickname ?? Configuration.SerialNumber}"));
			this.FTP = new BBLFTPConnection(fileStore, Logger.OfCategory($"BBL FTP {Configuration.Nickname ?? Configuration.SerialNumber}"));
			this.HasUSBOrSDCard = false;
			this.IsUSBOrSDCardRequired = BBLConstants.IsSDCardOrUSBRequired(BBLConstants.GetModelFromSerialNumber(configuration.SerialNumber));

			this.MQTT.OnData += MQTT_OnData;
			this.FTP.OnLocal3MFAdded += FTP_OnLocal3MFAdded;
			this.FTP.OnLocal3MFRemoved += FTP_OnLocal3MFRemoved;
			this.FTP.OnDisconnected += FTP_OnDisconnected;
			this.FTP.OnInitialScanComplete += FTP_OnInitialScanComplete;

			// TODO: Monitor MQTT & FTP connection to determine disconnection status.
		}

		public static BBLMachineConnection LAN(IMachineFileStore fileStore, BBLMachineConfiguration configuration)
		{
			return new BBLMachineConnection(fileStore, configuration);
		}

		#region Configuration

		public override BBLMachineConfiguration GetConfiguration()
		{
			return this.Configuration;
		}

		public async Task<MachineOperationResult> UpdateConfiguration(BBLMachineConfiguration updatedCfg)
		{
			// TODO: Test if works.

			var opResult = await Mono.MutateUntil(async () =>
			{
				// Reconfigure MQTT & FTP

				await this.MQTT.DisconnectAsync();
				await this.FTP.DisconnectAsync();

				this.Configuration = updatedCfg;

				await this.Connect_Internal();

			}, () => this.MQTT.IsConnected && this.FTP.IsConnected, TimeSpan.FromSeconds(30));

			return opResult.IntoOperationResult("bbl.config.update.failed", "Update Configuration");
		}

		public static Type GetConfigurationType()
		{
			return typeof(BBLMachineConfiguration);
		}

		public static BBLMachineConnection CreateFromConfiguration(IMachineFileStore fileStore, BBLMachineConfiguration configuration)
		{
			return LAN(fileStore, configuration);
		}

		#endregion

		protected override async Task<MachineOperationResult> Connect_Internal()
		{
			try
			{
				await this.MQTT.AutoConnectAsync(new BBLMQTTSettings(this.Configuration.Address.ToString(), this.Configuration.SerialNumber, this.Configuration.AccessCode, this.Model));
			}
			catch (Exception ex)
			{
				return MachineOperationResult.Fail("bbl.mqtt.connect.failed", "Unable to Connect to MQTT Broker", ex.Message, MachineMessageActions.CheckConfiguration, new MachineMessageAutoResole()
				{
					WhenConnected = true
				});
			}

			try
			{
				bool useGnuTls = BBLConstants.ModelFeatures.WithFTPSessionReuse.Contains(this.Model);
				await this.FTP.ConnectAsync(Configuration.Address, Configuration.AccessCode, this.ID, useGnuTls);
				CommitState(new MachineStateUpdate().SetIsLocalStorageScanning(true));
			}
			catch (Exception ex)
			{
				Logger.OfCategory($"BBL FTP {Configuration.Nickname ?? Configuration.SerialNumber}").Warning($"FTP connection failed — local job features disabled: {ex.Message}");
				AddNotification(BBLMessages.FTPDisconnected);
			}

			return MachineOperationResult.Ok;
		}

		public override async Task Disconnect()
		{
			await this.MQTT.DisconnectAsync();
			await this.FTP.DisconnectAsync();
		}

		protected override async Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream)
		{ 
			if (BBLFiles.TryParseAs3MFHandle(fileHandle, out var filePath))
			{
				await FTP.DownloadFile(filePath, destinationStream);
			}
			else if (BBLFiles.TryParseAs3MFThumbnailHandle(fileHandle, out filePath))
			{
				var (bbl3MF, _) = await FTP.Download3MF(filePath);

				if (bbl3MF.ThumbnailSmall == null)
				{
					throw new IOException("Thumbnail does not exist");
				}

				destinationStream.Write(bbl3MF.ThumbnailSmall, 0, bbl3MF.ThumbnailSmall.Length);
			}
			else
			{
				throw new IOException();
			}
		}

		protected override async Task PrintLocal_Internal(LocalPrintJob localPrint, PrintOptions options)
		{
			// Convert abstract PrintOptions to BBL-specific options
			Dictionary<int, AMSSlot>? amsMapping = null;

			if (options.MaterialMap != null && options.MaterialMap.Count > 0)
			{
				amsMapping = new Dictionary<int, AMSSlot>();

				foreach (var kvp in options.MaterialMap)
				{
					// Convert MMID (AMS serial number) to ams_id (integer)
					if (MQTT.TryGetAMSIdFromSN(kvp.Value.MUID, out int amsId))
					{
						amsMapping[kvp.Key] = new AMSSlot(amsId, kvp.Value.Slot);
					}
					else
					{
						throw new InvalidOperationException($"Unable to resolve AMS ID for Material Unit '{kvp.Value.MUID}'. The AMS mapping may not be available yet.");
					}
				}
			}

			if (!BBLFiles.TryParseAs3MFHandle(localPrint.File, out var localPath))
			{
				throw new InvalidOperationException("Select file must be 3MF");
			}

			var jobIdWithInfo = new PrefixedFixedLengthKeyValueMessage("Lib3Dp");
			jobIdWithInfo.Add("Path", localPath);
			jobIdWithInfo.Add("Hash", localPrint.File.HashSHA256);
			jobIdWithInfo.Add("Minutes", ((int)localPrint.Time.TotalMinutes).ToString());
			jobIdWithInfo.Add("Grams", localPrint.TotalGramsUsed.ToString());

			try
			{
				using var da3MF = BambuLab3MF.Load(await DownloadFile(localPrint.File));
				
				if (da3MF.ThumbnailSmallHash != null)
				{
					jobIdWithInfo.Add("ThumbnailSmallHash", da3MF.ThumbnailSmallHash);
				}
			}
			catch (Exception)
			{
				// Unable to download the 3MF to determine the Thumbnail Hash.
			}

			if (options.CustomID != null)
			{
				jobIdWithInfo.Add("CustomID", options.CustomID);
			}

			var bblPrintOptions = new BBLPrintOptions(
				PlateIndex: 1,
				FileName: localPath,
				SubTaskId: jobIdWithInfo.ToString(),
				ProjectFilamentCount: options.MaterialMap?.Count ?? 1,
				BedLeveling: options.LevelBed,
				FlowCalibration: options.FlowCalibration,
				VibrationCalibration: options.VibrationCalibration,
				LayerInspect: options.InspectFirstLayer,
				Timelapse: false,
				AMSMapping: amsMapping
			);

			await this.MQTT.PublishPrint(bblPrintOptions);
		}

		protected override Task Pause_Internal()
		{
			return this.MQTT.PublishPause();
		}

		protected override Task Resume_Internal()
		{
			return this.MQTT.PublishResume();
		}

		protected override Task Stop_Internal()
		{
			return this.MQTT.PublishStop();
		}

		protected override async Task BeginMUHeating_Internal(string unitID, HeatingSettings settings)
		{
			await MQTT.PublishAMSHeatingCommand(unitID, settings);
		}

		protected override async Task EndMaterialUnitHeating_Internal(string unitID)
		{
			await MQTT.PublishAMSStopHeatingCommand(unitID);
		}

		public override CameraSource GetCameraSource()
		{
			var model = this.State.Model;

			if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(model))
			{
				return new CameraSource.PullCameraSource(
					Upstream: new Uri($"rtsps://bblp:{Configuration.AccessCode}@{Configuration.Address}:322/streaming/live/1"),
					Spec: new CameraSpec(Width: 1920, Height: 1080, Fps: 30));
			}

			if (BBLConstants.ModelFeatures.With30FPMCamera.Contains(model))
			{
				return new CameraSource.PublisherCameraSource(
					Full: new StreamPublisherOptions(MaxWidth: null, MaxHeight: null, Crf: 23, GopSize: 1, Framerate: "1"),
					FullSpec: new CameraSpec(Width: 640, Height: 412, Fps: 20f / 60f),
					Glance: new StreamPublisherOptions(MaxWidth: 640,  MaxHeight: null, Crf: 30, GopSize: 1, Framerate: "1"),
					GlanceSpec: new CameraSpec(Width: 640, Height: 412, Fps: 20f / 60f));
			}

			return new CameraSource.NoCamera();
		}

		public override Task RunRTSPCameraPublisher(Uri rtspTarget, StreamPublisherOptions options, CancellationToken ct)
		{
			return BBLEspCameraPublisher.Run(Configuration.Address, Configuration.AccessCode, rtspTarget, options, Logger.OfCategory($"BBL Camera Publisher {Configuration.Nickname ?? Configuration.SerialNumber}"), ct);
		}

		protected override Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
		{
			return this.MQTT.PublishHVACModeCommand(mode);
		}

		protected override Task ToggleLight_Internal(string fixtureName, bool isOn)
		{
			var mqttNode = fixtureName switch
			{
				"Chamber" => "chamber_light",
				_ => fixtureName
			};
			return this.MQTT.PublishLEDControl(mqttNode, isOn);
		}

		protected override Task SetFanSpeed_Internal(string fanName, int speedPercent)
		{
			throw new NotSupportedException($"Setting fan speed is not implemented for Bambu Lab connections (requested {fanName} = {speedPercent}%).");
		}

		protected override Task SetPrintSpeed_Internal(int speedPercent)
		{
			return this.MQTT.PublishSetPrintSpeed(BBLConstants.SpeedPercentToBBLLevel(speedPercent));
		}

		protected override Task ClearBed_Internal()
		{
			return this.MQTT.PublishClearBed();
		}

		private void FTP_OnLocal3MFRemoved(string[] removedPaths)
		{
			var stateChanges = new MachineStateUpdate();

			foreach (var removed in removedPaths)
			{
				var jobToRemove = this.State.LocalJobs.FirstOrDefault(j => BBLFiles.TryParseAs3MFHandle(j.File, out var localPath) && localPath.Equals(removed, StringComparison.OrdinalIgnoreCase));

				if (jobToRemove == default) continue;

				Logger.OfCategory("BBL FTP").Trace($"Removed Local Job {removed} from State");

				stateChanges.RemoveLocalJobs(jobToRemove);
			}

			this.CommitState(stateChanges);
		}

		private void FTP_OnLocal3MFAdded(string fileName, string hash, BambuLab3MF bbl3MF)
		{
			try
			{
				var stateChanges = new MachineStateUpdate();

				var matsUsed = bbl3MF.Filaments.ToDictionary(f => f.Id, f => new MaterialToPrint(f.Filament, (int)f.UsedGrams, f.NozzleDiameter))!;

				var localJob = new LocalPrintJob(
					Path.GetFileNameWithoutExtension(fileName),
					BBLFiles.HandleAs3MF(this.ID, fileName, hash),
					(int)bbl3MF.TotalFilamentGrams,
					bbl3MF.PredictedTime,
					matsUsed);

				stateChanges.SetLocalJobs(localJob);

				this.CommitState(stateChanges);

				Logger.OfCategory("BBL FTP").Trace($"Added Local Job {localJob} to State");
			}
			catch (Exception ex)
			{
				Logger.OfCategory("BBL FTP").Error($"Failed to add Local Job {fileName} to State\n{ex}");
			}
		}

		private void MQTT_OnData(BBLMQTTData data)
		{
			if (data.HasUSBOrSDCard.HasValue && this.IsUSBOrSDCardRequired)
			{
				bool hasMedia = data.HasUSBOrSDCard.Value;
				bool hasMissingMessage = this.State.MappedNotifications.ContainsKey(BBLMessages.SDCardOrUSBMissing.Id);

				if (!hasMedia && !hasMissingMessage)
				{
					data.Changes.SetNotifications(BBLMessages.SDCardOrUSBMissing);
				}
				else if (hasMedia && hasMissingMessage)
				{
					data.Changes.RemoveNotifications(BBLMessages.SDCardOrUSBMissing.Id);
				}

				this.HasUSBOrSDCard = hasMedia;
			}

			if (data.Changes.StatusIsSet)
			{
				if (data.Changes.Status is MachineStatus.Disconnected)
				{
					// Disconnected, reset security flag.
					this.PrevUsesUnsupportedSecurity = false;
				}
			}

			if (data.FirmwareVersion.HasValue)
			{
				this.FirmwareVersion = data.FirmwareVersion.Value;
			}

			MachineCapabilities machineFeatures = MachineCapabilities.StartLocalJob | MachineCapabilities.LocalJobs | MachineCapabilities.Lighting | MachineCapabilities.Control | MachineCapabilities.PrintHistory;

			// We will decide which features are available on this machine depending on the model!

			string model = this.State.Model!;

			// All models have automatic bed-leveling capability.
			machineFeatures |= MachineCapabilities.Print_Options_BedLevel;

			if (BBLConstants.ModelFeatures.WithInspectFirstLayer.Contains(model))
			{
				machineFeatures |= MachineCapabilities.Print_Options_InspectFirstLayer;
			}

			if (BBLConstants.ModelFeatures.WithFlowRateCali.Contains(model))
			{
				machineFeatures |= MachineCapabilities.Print_Options_FlowCalibration;
			}

			machineFeatures |= MachineCapabilities.PrintSpeedControl;
			data.Changes.SetSpeedRange(BBLConstants.BBLSpeedRange);
			foreach (var (name, pct) in BBLConstants.BBLSpeedPresets)
			{
				data.Changes.SetSpeedPresets(name, pct);
			}

			if (BBLConstants.ModelFeatures.WithClimateControl.Contains(model))
			{
				machineFeatures |= MachineCapabilities.AirDuct;
			}

			if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(model)
				|| BBLConstants.ModelFeatures.With30FPMCamera.Contains(model))
			{
				machineFeatures |= MachineCapabilities.Camera;
			}

			// Apply runtime restrictions AFTER computing full capabilities

			if (data.UsesUnsupportedSecurity.HasValue && data.UsesUnsupportedSecurity.Value)
			{
				// Without LAN Mode and Developer Mode we cannot control anything.
				// When switching to these modes, the machine must be restarted.

				machineFeatures &= ~(MachineCapabilities.StartLocalJob | MachineCapabilities.Control | MachineCapabilities.AirDuct);
			}

			if (!this.HasUSBOrSDCard && this.IsUSBOrSDCardRequired)
			{
				machineFeatures &= ~(MachineCapabilities.StartLocalJob | MachineCapabilities.LocalJobs);
			}

			data.Changes.SetCapabilities(machineFeatures);

			// Remove HMS notifications that are no longer active
			if (data.ActiveHMSIds != null)
			{
				foreach (var (id, _) in this.State.Notifications)
				{
					if (id.StartsWith("bbl.hms.") && !data.ActiveHMSIds.Contains(id))
						data.Changes.RemoveNotifications(id);
				}
			}

			CommitState(data.Changes);
		}

		private void FTP_OnInitialScanComplete()
		{
			CommitState(new MachineStateUpdate().SetIsLocalStorageScanning(false));
		}

		private void FTP_OnDisconnected()
		{
			var stateUpdate = new MachineStateUpdate()
				.SetCapabilities(this.State.Capabilities & ~(MachineCapabilities.StartLocalJob | MachineCapabilities.LocalJobs))
				.SetIsLocalStorageScanning(false);

			if (this.HasUSBOrSDCard)
			{
				stateUpdate.SetNotifications(BBLMessages.FTPDisconnected);
			}
			else
			{
				stateUpdate.SetNotifications(BBLMessages.SDCardOrUSBMissing);
			}

			CommitState(stateUpdate);
		}
	}
}
