using Lib3Dp.State;

namespace Lib3Dp.Exceptions
{
	public class MachineException(MachineMessage message) : Exception("Machine encountered an Exception")
	{
		public MachineMessage MachineMessage { get; } = message;
	}
}
