using Connect3Dp.State;
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
        public static MachineMessage FailedToConnect => new("Unable to connect to Machine", "An issue occurred connecting to this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
        {
            AutoResolve = new MessageAutoResole
            {
                WhenConnected = true
            }
        };

        public static MachineMessage FailedToStop => new("Unable to stop the Machine", "An issue occurred stopping this Machine", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
        {
            AutoResolve = new MessageAutoResole
            {
                WhenStatus = MachineStatus.Canceled
            }
        };

        public static MachineMessage NoFeature(MachineCapabilities desiredFeature)
        {
            return new MachineMessage("Unsupported Feature", $"Machine does not support feature {Enum.GetName(desiredFeature)}", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
            {
                Severity = MachineMessageSeverity.ErrorDuringPrinting
            };
        }

        public static MachineMessage NoMUFeature(MaterialUnitFeatures desiredFeature)
        {
            return new MachineMessage("Unsupported Material Unit Feature", $"Material Unit does not support feature {Enum.GetName(desiredFeature)}", DateTime.Now, MessageSource.Connector, MachineMessageSeverity.ErrorDuringPrinting)
            {
                Severity = MachineMessageSeverity.ErrorDuringPrinting
            };
        }
    }
}