using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.State;

namespace Lib3Dp.Extensions
{
	public static class MaterialUnitExtensions
	{
		public static bool HasFeature(this IReadOnlyMaterialUnit state, MaterialUnitCapabilities desiredFeature)
		{
			return (state.Capabilities & desiredFeature) != MaterialUnitCapabilities.None;
		}

		/// <summary>
		/// <see cref="MachineException"/> is thrown when the current machine does not support the <paramref name="desiredFeature"/>
		/// </summary>
		/// <exception cref="MachineException"></exception>
		public static void EnsureHasFeature(this IReadOnlyMaterialUnit state, MaterialUnitCapabilities desiredFeature)
		{
			if (!state.HasFeature(desiredFeature))
			{
				throw new MachineException(MachineMessages.NoMUFeature(desiredFeature));
			}
		}
	}
}
