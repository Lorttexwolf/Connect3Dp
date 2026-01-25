using Connect3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Exceptions
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
