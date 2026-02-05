using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.Connectors.BambuLab.Files;
using Lib3Dp.Connectors.BambuLab.FTP;
using Lib3Dp.Connectors.BambuLab.MQTT;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Text;
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

		private BBLMachineConnection(string? nickname, string sn, string accessCode, IPAddress address) : base(nickname, sn, "BambuLab", BBLConstants.GetModelFromSerialNumber(sn))
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
		}

		private void FTP_OnLocal3MFRemoved(string[] obj)
		{
			var stateChanges = new MachineStateUpdate();

			foreach (var removed in obj)
			{
				var jobToRemove = this.State.LocalJobs.FirstOrDefault(j => j.Path.Equals(removed, StringComparison.OrdinalIgnoreCase));

				if (jobToRemove == null)
				{
					// We didn't have it in the first place?
					continue;
				}

				Console.WriteLine($"Removed Local Job {removed} from State");

				stateChanges.RemoveLocalJobs(jobToRemove);
			}

			this.CommitState(stateChanges);
		}

		private void FTP_OnLocal3MFAdded(string addedPath, BambuLab3MF bbl3MF)
		{
			try
			{
				var stateChanges = new MachineStateUpdate();

				var matsUsed = bbl3MF.Filaments.ToDictionary(f => f.Id, f => new MaterialToPrint(f.Filament, (int)f.UsedGrams, f.NozzleDiameter))!;

				var localJob = new LocalPrintJob(
					Path.GetFileNameWithoutExtension(addedPath), 
					addedPath, 
					(int)bbl3MF.TotalFilamentGrams, 
					bbl3MF.PredictedTime,
					matsUsed);

				stateChanges.SetLocalJobs(localJob);

				this.CommitState(stateChanges);

				Console.WriteLine($"Added Local Job {addedPath} to State");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to add Local Job {addedPath} to State\n{ex}");
			}
		}

		private void MQTT_OnData(BBLMQTTData data)
		{
			if (data.HasUSBOrSDCard.HasValue && this.IsUSBOrSDCardRequired)
			{
				bool hasMedia = data.HasUSBOrSDCard.Value;
				var hasMissingMessage = this.State.Messages.Contains(BBLMessages.SDCardOrUSBMissing);

				if (!hasMedia && !hasMissingMessage)
				{
					// Removed
					data.Changes.SetMessages(BBLMessages.SDCardOrUSBMissing);
				}
				else if (hasMedia && hasMissingMessage)
				{
					// Added
					data.Changes.RemoveMessages(BBLMessages.SDCardOrUSBMissing);
				}

				this.HasUSBOrSDCard = hasMedia;
			}

			if (data.Changes.IsConnected.HasValue)
			{
				if (!data.Changes.IsConnected.Value)
				{
					// Disconnected, reset security flag.
					this.PrevUsesUnsupportedSecurity = false;
				}
			}

			if (data.FirmwareVersion.HasValue)
			{
				this.FirmwareVersion = data.FirmwareVersion.Value;
			}

			MachineCapabilities machineFeatures = MachineCapabilities.Lighting | MachineCapabilities.Control | MachineCapabilities.FetchFiles | MachineCapabilities.PrintHistory;

			// We will decide which features are available on this machine depending on the model!

			string model = this.State.Model;

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

				machineFeatures &= ~(MachineCapabilities.SendJob | MachineCapabilities.StartLocalJob | MachineCapabilities.Control | MachineCapabilities.AirDuct);
			}

			if (!this.HasUSBOrSDCard && this.IsUSBOrSDCardRequired)
			{
				machineFeatures &= ~(MachineCapabilities.SendJob | MachineCapabilities.StartLocalJob | MachineCapabilities.FetchFiles);
			}

			data.Changes.SetCapabilities(machineFeatures);

			// Append Files to the Print Job

			var job = this.State.Job;

			if (job != null)
			{
				if (job.File == null)
				{
					// TODO: Verify the location of files on FTP.

					var file3MF = new BBLMachineFile(this, $"{job.Name}", "3MF");

					data.Changes.UpdateCurrentJob(c => c.SetFile(file3MF));
				}

				//if (job.Thumbnail == null)
				//{
				//	// TODO: Verify the location of files on FTP.

				//	var thumbnailFile = new BBLMachineThumbnailFile(this, $"{job.Name}");

				//	data.Changes.UpdateCurrentJob(c => c.SetThumbnail(thumbnailFile));
				//}
			}

			// Append to the Print History.

			if (data.Changes.Status.HasValue && this.State.Status == MachineStatus.Printing)
			{
				if (data.Changes.Status.Value == MachineStatus.Printed)
				{
					data.Changes.SetJobHistory(new HistoricPrintJob(job!.Name, true, DateTime.Now, job.TotalTime, job.Thumbnail, null));
				}
				else if (data.Changes.Status.Value == MachineStatus.Canceled)
				{
					data.Changes.SetJobHistory(new HistoricPrintJob(job!.Name, false, DateTime.Now, job.TotalTime - job.RemainingTime, job.Thumbnail, null));
				}
			}

			CommitState(data.Changes);
		}

		protected override async Task Connect_Internal(CancellationToken cancellationToken = default)
		{
			await Task.WhenAll(this.MQTT.Connect(cancellationToken), this.FTP.Connect());
		}

		protected override Task PrintLocal_Internal(string localPath, PrintOptions options, CancellationToken cancellationToken = default)
		{
			// Convert abstract PrintOptions to BBL-specific options
			Dictionary<int, AMSSlot>? amsMapping = null;
			
			if (options.MaterialMap != null && options.MaterialMap.Count > 0)
			{
				amsMapping = new Dictionary<int, AMSSlot>();
				
				foreach (var kvp in options.MaterialMap)
				{
					// Convert MMID (AMS serial number) to ams_id (integer)
					if (MQTT.TryGetAMSIdFromSN(kvp.Value.MMID, out int amsId))
					{
						amsMapping[kvp.Key] = new AMSSlot(amsId, kvp.Value.Slot);
					}
					else
					{
						throw new InvalidOperationException($"Unable to resolve AMS ID for Material Unit '{kvp.Value.MMID}'. The AMS mapping may not be available yet.");
					}
				}
			}

			var bblPrintOptions = new BBLPrintOptions(
				PlateIndex: 1,
				FileName: localPath,
				JobId: $"Lib3Dp-{DateTime.Now.Ticks}",
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

		protected override Task Pause_Internal(CancellationToken cancellationToken = default)
		{
			return this.MQTT.PublishPause();
		}

		protected override Task Resume_Internal(CancellationToken cancellationToken = default)
		{
			return this.MQTT.PublishResume();
		}

		protected override Task Stop_Internal(CancellationToken cancellationToken = default)
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

		protected override Task ClearBed_Internal(CancellationToken cancellationToken = default)
		{
			return this.MQTT.PublishClearBed();
		}

		public static BBLMachineConnection LAN(string? nickname, string sn, string accessCode, IPAddress address)
		{
			return new BBLMachineConnection(nickname, sn, accessCode, address);
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

		public static MachineConnection CreateFromConfiguration(object configuration)
		{
			if (configuration is BBLMachineConfiguration bbl)
			{
				return LAN(bbl.Nickname, bbl.SerialNumber, bbl.AccessCode, IPAddress.Parse(bbl.Address));
			}
			return null;
		}

		#endregion

		#region Files

		public class BBLMachineFile(BBLMachineConnection machine, string fileName, string appendedID) : MachineFile($"BBL/{machine.State.ID}/{appendedID}", machine)
		{
			public string FileName { get; } = fileName;
			private string AppendedID { get; } = appendedID;

			public override string? MimeType => "model/3mf";

			protected override Task<bool> DoDownload(Stream outStream)
			{
				return ((BBLMachineConnection)Machine).FTP.FTP.DownloadStream(outStream, FileName);
			}

			public override object Clone()
			{
				return new BBLMachineFile((BBLMachineConnection)this.Machine, this.FileName, this.AppendedID);
			}
		}

		//public class BBLMachineThumbnailFile(BBLMachineConnection machine, string File3MFName, int PlateNumber = 1, BambuLab3MF.ThumbnailPositions Position = BambuLab3MF.ThumbnailPositions.Diagonal) : BBLMachineFile(machine, File3MFName, "Thumbnail")
		//{
		//	public int PlateNumber { get; } = PlateNumber;
		//	public BambuLab3MF.ThumbnailPositions Position { get; } = Position;

		//	public override string? MimeType => "image/png";

		//	protected override async Task<bool> DoDownload(Stream outStream)
		//	{
		//		using var stream3MF = new MemoryStream();

		//		if (!await base.DoDownload(stream3MF)) return false;

		//		await BambuLab3MF.GetThumbnailEntry(stream3MF, outStream, Position, PlateNumber);

		//		return true;
		//	}

		//	public override object Clone()
		//	{
		//		return new BBLMachineThumbnailFile((BBLMachineConnection)this.Machine, this.FileName, this.PlateNumber, this.Position);
		//	}
		//}

		#endregion
	}
}
