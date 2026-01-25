using Connect3Dp.Constants;
using Connect3Dp.Exceptions;
using Connect3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Extensions
{
    public static class MaterialUnitExtensions
    {
        public static bool HasFeature(this IReadOnlyMaterialUnit state, MaterialUnitFeatures desiredFeature)
        {
            return (state.Features & desiredFeature) != MaterialUnitFeatures.None;
        }

        /// <summary>
        /// <see cref="MachineException"/> is thrown when the current machine does not support the <paramref name="desiredFeature"/>
        /// </summary>
        /// <exception cref="MachineException"></exception>
        public static void EnsureHasFeature(this IReadOnlyMaterialUnit state, MaterialUnitFeatures desiredFeature)
        {
            if (!state.HasFeature(desiredFeature))
            {
                throw new MachineException(MachineMessages.NoMUFeature(desiredFeature));
            }
        }
    }
}
