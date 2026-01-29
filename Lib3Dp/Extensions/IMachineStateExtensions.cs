using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.State;

namespace Lib3Dp.Extensions
{
	public static class IMachineStateExtensions
	{
		/// <summary>
		/// Ensures the given <see cref="MachinePrintOptions"/> assigned properties are supported by the machine or else a <see cref="NotSupportedException"/> is thrown.
		/// </summary>
		public static void EnsureSupportPrintOptions(this IReadOnlyMachineState state, PrintOptions options)
		{
			if (options.FlowCalibration && !state.HasFeature(MachineCapabilities.Print_Options_FlowCalibration))
			{
				throw new NotSupportedException("Flow Calibration is not supported as a print option");
			}
			else if (options.VibrationCalibration && !state.HasFeature(MachineCapabilities.Print_Options_VibrationCalibration))
			{
				throw new NotSupportedException("Vibration Calibration is not supported as a print option");
			}
			else if (options.LevelBed && !state.HasFeature(MachineCapabilities.Print_Options_BedLevel))
			{
				throw new NotSupportedException("Bed Leveling is not supported as a print options");
			}
			else if (options.InspectFirstLayer && !state.HasFeature(MachineCapabilities.Print_Options_InspectFirstLayer))
			{
				throw new NotSupportedException("Inspect First Layer is not supported as a print option");
			}
		}

		public static bool HasFeature(this IReadOnlyMachineState state, MachineCapabilities desiredFeature)
		{
			return state.Capabilities.HasFlag(desiredFeature);
		}

		/// <summary>
		/// <see cref="MachineException"/> is thrown when the current machine does not support the <paramref name="desiredFeature"/>
		/// </summary>
		/// <exception cref="MachineException"></exception>
		public static void EnsureHasFeature(this IReadOnlyMachineState state, MachineCapabilities desiredFeature)
		{
			if (!state.HasFeature(desiredFeature))
			{
				throw new MachineException(MachineMessages.NoFeature(desiredFeature));
			}
		}
	}
}
