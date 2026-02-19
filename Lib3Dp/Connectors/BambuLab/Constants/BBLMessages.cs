using Lib3Dp.State;

namespace Lib3Dp.Connectors.BambuLab.Constants
{
	internal static class BBLMessages
	{
		public static MachineMessage SDCardOrUSBMissing => new("SD Card or USB Drive is Missing", "An SD Card or USB Drive is required to send prints to this Machine.", MachineMessageSeverity.Warning, MachineMessageActions.None, default);
		public static MachineMessage FTPDisconnected => new("FTP Disconnected", SDCardOrUSBMissing.Body, MachineMessageSeverity.Warning, MachineMessageActions.None, default);
	}
}
