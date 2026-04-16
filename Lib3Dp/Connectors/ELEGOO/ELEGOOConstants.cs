using Lib3Dp.State;

namespace Lib3Dp.Connectors.ELEGOO
{
	/// <summary>
	/// SDCP (Smart Device Communication Protocol) command identifiers for ELEGOO printers.
	/// </summary>
	internal static class ELEGOOCmd
	{
		public const int GetStatus = 0;
		public const int GetAttributes = 1;
		public const int Disconnect = 64;
		public const int StartPrint = 128;
		public const int PausePrint = 129;
		public const int StopPrint = 130;
		public const int ResumePrint = 131;
		public const int GetBlackoutStatus = 134;
		public const int BlackoutAction = 135;
		public const int EditName = 192;
		public const int SendFileEnd = 255;
		public const int EditFileName = 257;
		public const int GetFileList = 258;
		public const int DeleteFile = 259;
		public const int GetFileDetail = 260;
		public const int GetHistoryIds = 320;
		public const int GetTaskDetail = 321;
		public const int DeleteHistory = 322;
		public const int GetHistoryVideo = 323;
		public const int EditVideoStreaming = 386;
		public const int EditTimeLapse = 387;
		public const int EditAxisNumber = 401;
		public const int EditAxisZero = 402;
		public const int EditStatusData = 403;
	}

	/// <summary>
	/// PrintInfo.Status values reported by the printer during/after a print.
	/// </summary>
	internal static class ELEGOOPrintStatus
	{
		public const int Idle = 0;
		public const int Stopping = 1;
		public const int Suspending = 5;
		public const int Suspended = 6;
		public const int Resuming = 7;
		public const int Stopped = 8;
		public const int Completed = 9;
		public const int FileDetection = 10;
		public const int Recovery = 12;
		public const int Printing = 13;
		public const int StoppedError = 14;
	}

	/// <summary>
	/// CurrentStatus array values from the sdcp/status broadcast.
	/// </summary>
	internal static class ELEGOOCurrentStatus
	{
		public const int Idle = 0;
		public const int Printing = 1;
		public const int FileTransferring = 8;
	}

	internal static class ELEGOOConstants
	{
		public static readonly HeatingConstraints BedConstraints = new(0, 120);
		public static readonly HeatingConstraints NozzleConstraints = new(0, 300);
		public static readonly HeatingConstraints ChamberConstraints = new(0, 70);

		public static string GetModelName(ELEGOOMachineKind kind) => kind switch
		{
			ELEGOOMachineKind.CentauriCarbon => "Centauri Carbon",
			ELEGOOMachineKind.CentauriCarbon2 => "Centauri Carbon 2",
			_ => "Centauri Carbon"
		};

		public static MachineCapabilities GetCapabilities(ELEGOOMachineKind model)
		{
			var caps = MachineCapabilities.StartLocalJob
				| MachineCapabilities.Control
				| MachineCapabilities.Lighting
				| MachineCapabilities.Temps
				| MachineCapabilities.Fans
				| MachineCapabilities.LocalJobs;

			return caps;
		}

		public static MachineStatus MapPrintStatus(int elegooStatus) => elegooStatus switch
		{
			ELEGOOPrintStatus.Idle => MachineStatus.Idle,
			ELEGOOPrintStatus.Printing => MachineStatus.Printing,
			ELEGOOPrintStatus.FileDetection => MachineStatus.Printing,
			ELEGOOPrintStatus.Suspending => MachineStatus.Printing,
			ELEGOOPrintStatus.Resuming => MachineStatus.Printing,
			ELEGOOPrintStatus.Suspended => MachineStatus.Paused,
			ELEGOOPrintStatus.Recovery => MachineStatus.Paused,
			ELEGOOPrintStatus.Completed => MachineStatus.Printed,
			ELEGOOPrintStatus.Stopped => MachineStatus.Canceled,
			ELEGOOPrintStatus.StoppedError => MachineStatus.Canceled,
			ELEGOOPrintStatus.Stopping => MachineStatus.Printing,
			_ => MachineStatus.Idle
		};
	}
}
