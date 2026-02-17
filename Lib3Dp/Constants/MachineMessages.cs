using Lib3Dp.State;

namespace Lib3Dp.Constants
{
	/// <summary>
	/// Generic machine messages.
	/// </summary>
	internal static class MachineMessages
	{
		public static MachineMessage FailedToConnect => new(
			"Unable to connect to Machine",
			"An issue occurred connecting to this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenConnected = true });

		public static MachineMessage FailedToPause => new(
			"Unable to Pause",
			"An issue occurred pausing this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Paused });

		public static MachineMessage FailedToResume => new(
			"Unable to Resume",
			"An issue occurred resuming this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Printing });

		public static MachineMessage FailedToStop => new(
			"Unable to stop the Machine",
			"An issue occurred stopping this Machine",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Canceled });

		public static MachineMessage FailedToClearBed => new(
			"Unable to Clear Bed",
			"An issue occurred clearing the build plate",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			new MachineMessageAutoResole { WhenStatus = MachineStatus.Idle });

		public static MachineMessage FailedToStartLocalPrint => new(
			"Unable to Start Local Print",
			"An issue occurred starting a local print",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToBeginMUHeating => new(
			"Unable to Begin Material Unit Heating",
			"An issue occurred starting material unit heating",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToEndMUHeating => new(
			"Unable to End Material Unit Heating",
			"An issue occurred stopping material unit heating",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage FailedToChangeAirDuct => new(
			"Unable to Change Air Duct Mode",
			"An issue occurred changing the air duct mode",
			MachineMessageSeverity.Error,
			MachineMessageActions.None,
			default);

		public static MachineMessage MUDoesNotExist(string unitID) => new(
			"Material Unit does not Exist",
			$"Material Unit of ID {unitID} does not exist",
			MachineMessageSeverity.Error,
			MachineMessageActions.Refresh,
			default);

		public static MachineMessage NoFeature(MachineCapabilities desiredFeature)
		{
			return new MachineMessage(
				"Unsupported Feature",
				$"Machine does not support feature {Enum.GetName(desiredFeature)}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage NoMUFeature(MUCapabilities desiredFeature)
		{
			return new MachineMessage(
				"Unsupported Material Unit Feature",
				$"Material Unit does not support feature {Enum.GetName(desiredFeature)}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage ScheduledPrintSkipped(string jobName, string reason)
		{
			return new MachineMessage(
				"Scheduled Print Skipped",
				$"The scheduled print '{jobName}' was skipped: {reason}",
				MachineMessageSeverity.Warning,
				MachineMessageActions.None,
				default);
		}

		public static MachineMessage ScheduledPrintFailed(string jobName, string error)
		{
			return new MachineMessage(
				"Scheduled Print Failed",
				$"The scheduled print '{jobName}' failed to start: {error}",
				MachineMessageSeverity.Error,
				MachineMessageActions.None,
				default);
		}
	}
}
