using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.State
{
    public readonly record struct MaterialLocation(string MUID, int Slot)
    {
        public override string ToString()
        {
            return $"MU {MUID} Slot {Slot}";
        }
    }
}
