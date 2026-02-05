using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.State;

namespace Lib3Dp.Extensions
{
	public record MaterialMatchResult(Dictionary<int, List<SpoolLocation>> MaterialMatchesAndSimilarColor, Dictionary<int, List<SpoolLocation>> MaterialOnlyMatches);

	public static class IMachineStateExtensions
	{
		/// <summary>
		/// Finds available spools that match the required materials for a print job.
		/// </summary>
		public static MaterialMatchResult FindMatchingSpools(this IReadOnlyMachineState state, Dictionary<int, MaterialToPrint> materialsToPrint)
		{
			var similarColorMatches = new Dictionary<int, List<SpoolLocation>>();
			var materialOnlyMatches = new Dictionary<int, List<SpoolLocation>>();

			foreach (var (projectFilamentId, materialToPrint) in materialsToPrint)
			{
				var similarColorSpools = new List<SpoolLocation>();
				var materialOnlySpools = new List<SpoolLocation>();

				foreach (var mu in state.MaterialUnits)
				{
					foreach (var spool in mu.Trays)
					{
						if (!spool.Material.Name.Equals(materialToPrint.Material.Name, StringComparison.OrdinalIgnoreCase)) continue;

						var location = new SpoolLocation(mu.ID, spool.Number);
						bool colorMatches = spool.Material.Color.IsSimilarTo(materialToPrint.Material.Color);

						if (colorMatches) similarColorSpools.Add(location);
						else materialOnlySpools.Add(location);
					}
				}

				similarColorMatches[projectFilamentId] = similarColorSpools;
				materialOnlyMatches[projectFilamentId] = materialOnlySpools;
			}

			return new MaterialMatchResult(similarColorMatches, materialOnlyMatches);
		}

		public static void EnsureSupportPrintOptions(this IReadOnlyMachineState state, PrintOptions options)
		{
			if (options.FlowCalibration && !state.HasFeature(MachineCapabilities.Print_Options_FlowCalibration))
				throw new NotSupportedException("Flow Calibration is not supported as a print option");

			if (options.VibrationCalibration && !state.HasFeature(MachineCapabilities.Print_Options_VibrationCalibration))
				throw new NotSupportedException("Vibration Calibration is not supported as a print option");

			if (options.LevelBed && !state.HasFeature(MachineCapabilities.Print_Options_BedLevel))
				throw new NotSupportedException("Bed Leveling is not supported as a print option");

			if (options.InspectFirstLayer && !state.HasFeature(MachineCapabilities.Print_Options_InspectFirstLayer))
				throw new NotSupportedException("Inspect First Layer is not supported as a print option");
		}

		public static bool HasFeature(this IReadOnlyMachineState state, MachineCapabilities desiredFeature)
			=> state.Capabilities.HasFlag(desiredFeature);

		/// <summary>
		/// Throws <see cref="MachineException"/> when the machine does not support the <paramref name="desiredFeature"/>.
		/// </summary>
		/// <exception cref="MachineException">Thrown when the feature is not supported.</exception>
		public static void EnsureHasFeature(this IReadOnlyMachineState state, MachineCapabilities desiredFeature)
		{
			if (!state.HasFeature(desiredFeature))
				throw new MachineException(MachineMessages.NoFeature(desiredFeature));
		}
	}
}
