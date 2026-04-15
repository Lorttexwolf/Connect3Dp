using Lib3Dp.State;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public record struct AtAGlanceMachineState(
		MachineStatus Status,
		MachineCapabilities Capabilities,
		string? Nickname,
		IMachinePrintJob? Job)
	{
		public static AtAGlanceMachineState Of(IMachineState machineState) => new(
			machineState.Status,
			machineState.Capabilities,
			machineState.Nickname,
			machineState.CurrentJob);
	}
}
