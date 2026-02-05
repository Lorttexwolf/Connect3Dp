using Lib3Dp.State;

namespace Lib3Dp.Constants
{
	/// <summary>
	/// Generic machine messages.
	/// </summary>
	internal static class MachineMessages
	{
		public static MachineMessage FailedToConnect => new("Unable to connect to Machine", "An issue occurred connecting to this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
		{
			AutoResolve = new MessageAutoResole
			{
				WhenConnected = true
			}
		};

		public static MachineMessage FailedToPause => new("Unable to Pause", "An issue occurred pausing this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
		{
			AutoResolve = new MessageAutoResole
			{
				WhenStatus = MachineStatus.Paused
			}
		};

		public static MachineMessage FailedToResume => new("Unable to Resume", "An issue occurred resuming this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
		{
			AutoResolve = new MessageAutoResole
			{
				WhenStatus = MachineStatus.Printing
			}
		};

		public static MachineMessage FailedToStop => new("Unable to stop the Machine", "An issue occurred stopping this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
		{
			AutoResolve = new MessageAutoResole
			{
				WhenStatus = MachineStatus.Canceled
			}
		};

		public static MachineMessage FailedToClearBed => new("Unable to Clear Bed", "An issue occurred clearing the build plate", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
		{
			AutoResolve = new MessageAutoResole
			{
				WhenStatus = MachineStatus.Idle
			}
		};

		public static MachineMessage NoFeature(MachineCapabilities desiredFeature)
		{
			return new MachineMessage("Unsupported Feature", $"Machine does not support feature {Enum.GetName(desiredFeature)}", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
			{
				Severity = MachineMessageSeverity.ErrorDuringPrinting
			};
		}

		public static MachineMessage NoMUFeature(MaterialUnitCapabilities desiredFeature)
		{
			return new MachineMessage("Unsupported Material Unit Feature", $"Material Unit does not support feature {Enum.GetName(desiredFeature)}", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
			{
				Severity = MachineMessageSeverity.ErrorDuringPrinting
			};
		}

		public static MachineMessage ScheduledPrintSkipped(string jobName, string reason)
		{
			return new MachineMessage(
				"Scheduled Print Skipped",
				$"The scheduled print '{jobName}' was skipped: {reason}",
				DateTime.Now,
				MessageSource.Connector,
				MachineMessageSeverity.Warning);
		}

		public static MachineMessage ScheduledPrintFailed(string jobName, string error)
		{
			return new MachineMessage(
				"Scheduled Print Failed",
				$"The scheduled print '{jobName}' failed to start: {error}",
				DateTime.Now,
				MessageSource.Connector,
				MachineMessageSeverity.ErrorDuringPrinting);
		}
	}
}
