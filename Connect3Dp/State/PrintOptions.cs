using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.State
{
    public record struct PrintOptions(bool LevelBed, bool FlowCalibration, bool VibrationCalibration, bool InspectFirstLayer);
}
