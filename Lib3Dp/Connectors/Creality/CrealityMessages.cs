using Lib3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp.Connectors.Creality
{
    internal class CrealityMessages
    {
        public static MachineMessage SDCardOrUSBMissing => new("creality.sdcard.missing", "SD Card or USB Drive is Missing", "An SD Card or USB Drive is required to send prints to this Machine.", MachineMessageSeverity.Warning, MachineMessageActions.None, default);
    }
}
