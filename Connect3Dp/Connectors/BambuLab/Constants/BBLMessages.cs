using Connect3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.BambuLab.Constants
{
    internal static class BBLMessages
    {
        public static MachineMessage SDCardOrUSBMissing => new("SD Card or USB Drive is Missing", "An SD Card or USB Drive is required to sent prints to this Machine.", DateTime.Now, MessageSource.Machine, MachineMessageSeverity.Warning);
    }
}
