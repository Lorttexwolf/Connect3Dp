using Lib3Dp.State;

namespace Lib3Dp.Exceptions
{
	public class MachineException : Exception
	{
		public MachineMessage MachineMessage { get; }

		public MachineException(MachineMessage message, Exception? causingException = null) : base("Machine encountered an Exception", causingException)
		{
			message.ProgramException = this;
			MachineMessage = message;
		}
	}
}
