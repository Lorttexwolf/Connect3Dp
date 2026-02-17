using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.Connectors.BambuLab.FTP;
using Lib3Dp.Connectors.BambuLab.MQTT;
using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Serialization;

namespace Lib3Dp.Connectors.BambuLab
{
	public sealed class BBLMachineConfiguration : IConnectorConfiguration
	{
		public required string Address { get; init; }
		public required string SerialNumber { get; init; }
		public required string AccessCode { get; init; }
		public string? Nickname { get; init; }

		public string ConnectorTypeFullName { get; } = typeof(BBLMachineConnection).FullName!;
	}

	public class BBLMachineConnection : MachineConnection, IConfigurableConnector
	{
		public IPAddress Address { get; }
		public string SerialNumber { get; }
		public string PrefixSerialNumber { get; }
		public string AccessCode { get; }

		private readonly BBLFTPConnection FTP;
		private readonly BBLMQTTConnection MQTT;
		private BBLFirmwareVersion? FirmwareVersion;
		private bool PrevUsesUnsupportedSecurity;
		private bool HasUSBOrSDCard;
		private readonly bool IsUSBOrSDCardRequired;

		private BBLMachineConnection(IMachineFileStore fileStore, string? nickname, string sn, string accessCode, IPAddress address) : base(fileStore, nickname, sn, "BambuLab", BBLConstants.GetModelFromSerialNumber(sn))
		{
			this.Address = address;
			this.SerialNumber = sn;
			this.PrefixSerialNumber = sn[..3];
			this.AccessCode = accessCode;
			this.MQTT = new BBLMQTTConnection(address, sn, accessCode);
			this.FTP = new BBLFTPConnection(address, accessCode);
			this.HasUSBOrSDCard = false;
			this.IsUSBOrSDCardRequired = BBLConstants.IsSDCardOrUSBRequired(BBLConstants.GetModelFromSerialNumber(sn));

			this.MQTT.OnData += MQTT_OnData;
			this.FTP.OnLocal3MFAdded += FTP_OnLocal3MFAdded;
			this.FTP.OnLocal3MFRemoved += FTP_OnLocal3MFRemoved;

			// TODO: Monitor MQTT & FTP connection to determine disconnection status.
		}

		public static BBLMachineConnection LAN(IMachineFileStore fileStore, string? nickname, string sn, string accessCode, IPAddress address)
		{
			return new BBLMachineConnection(fileStore, nickname, sn, accessCode, address);
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

		protected override async Task Connect_Internal()
		{
			await Task.WhenAll(this.MQTT.ConnectAsync(), this.FTP.ConnectAsync());
		}

		protected override Task PrintLocal_Internal(LocalPrintJob localPrint, PrintOptions options)
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
			jobIdWithInfo.Add("Minutes", ((int)localPrint.Time.TotalMinutes).ToString());
			jobIdWithInfo.Add("Grams", localPrint.TotalGramsUsed.ToString());

			var bblPrintOptions = new BBLPrintOptions(
				PlateIndex: 1,
				FileName: localPrint.Name,
				MetadataId: jobIdWithInfo.ToString(),
				ProjectFilamentCount: options.MaterialMap?.Count ?? 1,
				BedLeveling: options.LevelBed,
				FlowCalibration: options.FlowCalibration,
				VibrationCalibration: options.VibrationCalibration,
				LayerInspect: options.InspectFirstLayer,
				Timelapse: false,
				AMSMapping: amsMapping
			);

			return this.MQTT.PublishPrint(bblPrintOptions);
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

		internal override bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
		{
			if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(this.State.Model))
			{
				// RTSPS
				passURL = $"rtsps://bblp:{AccessCode}@{Address}:322/streaming/live/1";
			}
			else
			{
				// TODO: A1 Mini, A1, P1P, P1S
				passURL = null;
			}
			return passURL != null;
		}

		protected override Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
		{
			return this.MQTT.PublishHVACModeCommand(mode);
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

				Console.WriteLine($"Removed Local Job {removed} from State");

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
					BBLFiles.HandleAs3MF(this.State.ID, fileName, hash),
					(int)bbl3MF.TotalFilamentGrams,
					bbl3MF.PredictedTime,
					matsUsed);

				stateChanges.SetLocalJobs(localJob);

				this.CommitState(stateChanges);

				Console.WriteLine($"Added Local Job {localJob} to State");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to add Local Job {fileName} to State\n{ex}");
			}
		}

		private void MQTT_OnData(BBLMQTTData data)
		{
			// TODO: Reimplement
			//if (data.HasUSBOrSDCard.HasValue && this.IsUSBOrSDCardRequired)
			//{
			//	bool hasMedia = data.HasUSBOrSDCard.Value;
			//	var hasMissingMessage = this.State.Notifications.FirstOrDefault(BBLMessages.SDCardOrUSBMissing);

			//	if (!hasMedia && !hasMissingMessage)
			//	{
			//		// Removed
			//		data.Changes.SetMessages(BBLMessages.SDCardOrUSBMissing);
			//	}
			//	else if (hasMedia && hasMissingMessage)
			//	{
			//		// Added
			//		data.Changes.RemoveMessages(BBLMessages.SDCardOrUSBMissing);
			//	}

			//	this.HasUSBOrSDCard = hasMedia;
			//}

			if (data.Changes.IsConnectedIsSet)
			{
				if (!data.Changes.IsConnected)
				{
					// Disconnected, reset security flag.
					this.PrevUsesUnsupportedSecurity = false;
				}
			}

			if (data.FirmwareVersion.HasValue)
			{
				this.FirmwareVersion = data.FirmwareVersion.Value;
			}

			MachineCapabilities machineFeatures = MachineCapabilities.StartLocalJob | MachineCapabilities.Lighting | MachineCapabilities.Control | MachineCapabilities.PrintHistory;

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

			if (BBLConstants.ModelFeatures.WithClimateControl.Contains(model))
			{
				machineFeatures |= MachineCapabilities.AirDuct;
			}

			if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(model))
			{
				// TODO: Implement

				//machineFeatures |= MachineCapabilities.OME;
			}

			if (BBLConstants.ModelFeatures.With30FPMCamera.Contains(model)) // Lovely, 30 Frames per Minute.
			{
				// TODO: Implement
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
				machineFeatures &= ~(MachineCapabilities.StartLocalJob);
			}

			data.Changes.SetCapabilities(machineFeatures);

			CommitState(data.Changes);
		}

		#region Configuration

		public override object GetConfiguration()
		{
			return new BBLMachineConfiguration()
			{
				Nickname = this.State.Nickname,
				AccessCode = AccessCode,
				Address = Address.ToString(),
				SerialNumber = SerialNumber,
			};
		}

		public static Type GetConfigurationType()
		{
			return typeof(BBLMachineConfiguration);
		}

		public static MachineConnection CreateFromConfiguration(IMachineFileStore fileStore, object configuration)
		{
			if (configuration is BBLMachineConfiguration bbl)
			{
				return LAN(fileStore, bbl.Nickname, bbl.SerialNumber, bbl.AccessCode, IPAddress.Parse(bbl.Address));
			}
			return null;
		}

		#endregion
	}
}
