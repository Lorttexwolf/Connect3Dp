using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.State;
using System.Diagnostics.CodeAnalysis;

namespace Lib3Dp.Extensions
{
	public static class MaterialUnitExtensions
	{
		public static bool HasFeature(this IMaterialUnit state, MUCapabilities desiredFeature)
		{
			return (state.Capabilities & desiredFeature) != MUCapabilities.None;
		}

		public static bool IfNotCapable(this IMaterialUnit state, MUCapabilities desiredFeature, [NotNullWhen(true)] out MachineOperationResult? operationResult)
		{
			if (state.HasFeature(desiredFeature))
			{
				operationResult = null;
				return false;
			}

			operationResult = MachineOperationResult.Fail(MachineMessages.NoMUFeature(desiredFeature));
			return true;
		}
	}
}
