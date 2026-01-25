using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.State
{
    public enum MachineAirDuctMode
    {
        /// <summary>
        /// For example, the Bambu Lab P2S air conditioning in cooling mode.
        /// </summary>
        Cooling = 1,
        /// <summary>
        /// For example, the Bambu Lab P2S air conditioning in heating mode.
        /// </summary>
        Heating = 2
    }
}
