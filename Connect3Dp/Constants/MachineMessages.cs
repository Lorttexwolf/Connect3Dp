using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Constants
{
    /// <summary>
    /// Generic machine messages.
    /// </summary>
    internal static class MachineMessages
    {
        public static MachineMessage FailedToConnect => new("Unable to connect to Machine", "An issue occurred connecting to this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.Error)
        {
            AutoResolve = new MessageAutoResole
            {
                OnConnected = true
            }
        };

        public static MachineMessage NoFeature(MachineFeature desiredFeature)
        {
            return new MachineMessage("Unsupported Feature", $"Machine does not support feature {Enum.GetName(desiredFeature)}", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.Error)
            {
                Severity = MachineMessageSeverity.Error
            };
        }
    }
}