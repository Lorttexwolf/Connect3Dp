using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace Lib3Dp.Connectors.BambuLab
{
	public sealed class BBLMachineConfiguration : IConnectorConfiguration
	{
		public required string Address { get; init; }
		public required string SerialNumber { get; init; }
		public required string AccessCode { get; init; }
		public string? Nickname { get; init; }

		public string ConnectorTypeFullName { get; } = typeof(BBLMachineConnector).FullName!;
	}

	public class BBLMachineConnector : MachineConnection, IConfigurableConnector
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

		private BBLMachineConnector(string? nickname, string sn, string accessCode, IPAddress address) : base(nickname, sn, "BambuLab", BBLConstants.GetModelFromSerialNumber(sn))
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

			_ = new PeriodicAsyncAction(TimeSpan.FromSeconds(30), async () =>
			{
				if (!this.State.IsConnected) return;

				var sb = new StringBuilder("3MF Files on SD Card / USB:\n");

				foreach (var item3MF in await this.FTP.List3MFFiles())
				{
					sb.AppendLine($"|\t{item3MF}");
				}

				Logger.OfCategory("Bleh, 3MF").Trace(sb.ToString());
			});
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

				if (job.Thumbnail == null)
				{
					// TODO: Verify the location of files on FTP.

					var thumbnailFile = new BBLMachineThumbnailFile(this, $"{job.Name}");

					data.Changes.UpdateCurrentJob(c => c.SetThumbnail(thumbnailFile));
				}
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

		public static BBLMachineConnector LAN(string? nickname, string sn, string accessCode, IPAddress address)
		{
			return new BBLMachineConnector(nickname, sn, accessCode, address);
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

		public class BBLMachineFile(BBLMachineConnector machine, string fileName, string appendedID) : MachineFile($"BBL/{machine.State.ID}/{appendedID}", machine)
		{
			public string FileName { get; } = fileName;
			private string AppendedID { get; } = appendedID;

			public override string? MimeType => "model/3mf";

			protected override Task<bool> DoDownload(Stream outStream)
			{
				return ((BBLMachineConnector)Machine).FTP.DownloadStream(outStream, FileName);
			}

			public override object Clone()
			{
				return new BBLMachineFile((BBLMachineConnector)this.Machine, this.FileName, this.AppendedID);
			}
		}

		public class BBLMachineThumbnailFile(BBLMachineConnector machine, string File3MFName, int PlateNumber = 1, BBLMachineThumbnailFile.Positions Position = BBLMachineThumbnailFile.Positions.Diagonal) : BBLMachineFile(machine, File3MFName, "Thumbnail")
		{
			public int PlateNumber { get; } = PlateNumber;
			public Positions Position { get; } = Position;

			public override string? MimeType => "image/png";

			protected override async Task<bool> DoDownload(Stream outStream)
			{
				using var stream3MF = new MemoryStream();

				if (!await base.DoDownload(stream3MF)) return false;

				stream3MF.Position = 0;

				using var zip = new ZipArchive(stream3MF, ZipArchiveMode.Read);

				var entry = zip.GetEntry(GetThumbnailEntryPath());
				if (entry is null) return false;

				if (outStream.CanSeek) outStream.SetLength(0);

				using var entryStream = entry.Open();
				await entryStream.CopyToAsync(outStream);
				await outStream.FlushAsync();

				return true;
			}

			private string GetThumbnailEntryPath() => $"Metadata/{Position switch
			{
				Positions.Diagonal => $"plate_{PlateNumber}_small",
				Positions.Top => $"top_{PlateNumber}",
				_ => throw new ArgumentOutOfRangeException(nameof(Position))
			}}.png";

			public override object Clone()
			{
				return new BBLMachineThumbnailFile((BBLMachineConnector)this.Machine, this.FileName, this.PlateNumber, this.Position);
			}

			public enum Positions
			{
				Diagonal = 0,
				Top = 1
			}
		}

		#endregion
	}
}
