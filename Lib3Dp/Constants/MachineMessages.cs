using Lib3Dp.State;

namespace Lib3Dp.Constants
{
	/// <summary>
	/// Generic machine messages.
	/// </summary>
	internal static class MachineMessages
	{
		public static MachineMessage DisconnectedMessage => new(
			"machine.disconnected",
			"Disconnected from Machine",
			"Unable to establish connection to Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.CheckConfiguration,
			new MachineMessageAutoResole { WhenConnected = true });

        public static MachineMessage SDCardOrUSBMissing => new(
			"machine.sdcard.missing", 
			"SD Card or USB Drive is Missing", 
			"An SD Card or USB Drive is required to send prints to this Machine.", 
			MachineMessageSeverity.Warning, 
			MachineMessageActions.None, 
			default);

        public static MachineMessage FailedToConnect => new(
			"machine.connect.failed",
			"Unable to connect to Machine",
			"An issue occurred connecting to this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenConnected = true });

		public static MachineMessage FailedToPause => new(
			"machine.pause.failed",
			"Unable to Pause",
			"An issue occurred pausing this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Paused });

		public static MachineMessage FailedToResume => new(
			"machine.resume.failed",
			"Unable to Resume",
			"An issue occurred resuming this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Printing });

		public static MachineMessage FailedToStop => new(
			"machine.stop.failed",
			"Unable to stop the Machine",
			"An issue occurred stopping this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Canceled });

		public static MachineMessage FailedToClearBed => new(
			"machine.clearbed.failed",
			"Unable to Clear Bed",
			"An issue occurred clearing the build plate",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Idle });

		public static MachineMessage FailedToStartLocalPrint => new(
			"machine.localprint.failed",
			"Unable to Start Local Print",
			"An issue occurred starting a local print",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToBeginMUHeating => new(
			"machine.mu.heating.begin.failed",
			"Unable to Begin Material Unit Heating",
			"An issue occurred starting material unit heating",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToEndMUHeating => new(
			"machine.mu.heating.end.failed",
			"Unable to End Material Unit Heating",
			"An issue occurred stopping material unit heating",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToChangeAirDuct => new(
			"machine.airduct.failed",
			"Unable to Change Air Duct Mode",
			"An issue occurred changing the air duct mode",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToToggleLight => new(
			"machine.light.failed",
			"Unable to Toggle Light",
			"An issue occurred toggling the light",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToSetFanSpeed => new(
			"machine.fan.failed",
			"Unable to Set Fan Speed",
			"An issue occurred changing fan speed",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage MUDoesNotExist(string unitID) => new(
			$"machine.mu.notfound.{unitID}",
			"Material Unit does not Exist",
			$"Material Unit of ID {unitID} does not exist",
			MachineMessageSeverity.Error,
			MachineMessageActions.Refresh,
			default);

		public static MachineMessage NoFeature(MachineCapabilities desiredFeature)
		{
			return new MachineMessage(
				$"machine.feature.unsupported.{Enum.GetName(desiredFeature)}",
				"Unsupported Feature",
				$"Machine does not support feature {Enum.GetName(desiredFeature)}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage NoMUFeature(MUCapabilities desiredFeature)
		{
			return new MachineMessage(
				$"machine.mu.feature.unsupported.{Enum.GetName(desiredFeature)}",
				"Unsupported Material Unit Feature",
				$"Material Unit does not support feature {Enum.GetName(desiredFeature)}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage FileNotOnMachine(string uri) => new(
			"machine.file.not.found",
			"File Not Found",
			$"'{uri}' is not on the printer and is not available in the file store.",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage UploadNotSupported(string connectorName) => new(
			"machine.upload.unsupported",
			"Upload Not Supported",
			$"{connectorName} does not support uploading files to the machine.",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage ScheduledPrintSkipped(string jobName, string reason)
		{
			return new MachineMessage(
				$"machine.scheduled.skipped.{jobName}",
				"Scheduled Print Skipped",
				$"The scheduled print '{jobName}' was skipped: {reason}",
				MachineMessageSeverity.Warning,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage ScheduledPrintFailed(string jobName, string error)
		{
			return new MachineMessage(
				$"machine.scheduled.failed.{jobName}",
				"Scheduled Print Failed",
				$"The scheduled print '{jobName}' failed to start: {error}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}
	}
}
