using Lib3Dp.Constants;
using Lib3Dp.Exceptions;
using Lib3Dp.State;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lib3Dp.Extensions
{
	public record struct SpoolMatch(SpoolLocation Location, Material MaterialInMatchedSpool, double DeltaE);

	public record struct Matches<K, V>(Dictionary<K , List<V>> All, Dictionary<K, V> Match, HashSet<K> Missing) where K : notnull
	{
		public readonly bool HasMissing => Missing != null && Missing.Count > 0;
	}

	public static class IMachineStateExtensions
	{
		/// <summary>
		/// Finds available spools that match the required materials for a print job.
		/// </summary>
		public static Matches<int, SpoolMatch> FindMatchingSpools(this IMachineState state, IDictionary<int, MaterialToPrint> materialsToPrint)
		{
			var similarColorMatches = new Dictionary<int, List<SpoolMatch>>();

			foreach (var (projectFilamentId, materialToPrint) in materialsToPrint)
			{
				similarColorMatches[projectFilamentId] = [];

				foreach (var mu in state.MaterialUnits)
				{
					foreach (var (_, spool) in mu.Trays)
					{
						if (!spool.Material.Name.Equals(materialToPrint.Material.Name, StringComparison.OrdinalIgnoreCase)) continue;

						var location = new SpoolLocation(mu.ID, spool.Number);

						bool colorMatches = spool.Material.Color.IsSimilarTo(materialToPrint.Material.Color, out double deltaE);

						if (colorMatches)
						{
							similarColorMatches[projectFilamentId].Add(new SpoolMatch(location, spool.Material, deltaE));
						}
						else
						{
							continue;
						}
					}
				}
			}

			// Filter only entries that have matches
			var allMatches = similarColorMatches.Where(c => c.Value.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

			var bestMatches = allMatches.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.OrderBy(m => m.DeltaE).First());

			var missing = similarColorMatches.Where(c => c.Value.Count == 0).Select(kvp => kvp.Key).ToHashSet();

			return new Matches<int, SpoolMatch>(allMatches, bestMatches, missing);
		}

		/// <summary>
		/// Given a previously mapped dictionary of Filament's to Spool Locations, check if it's still valid or can be modified to recover.
		/// </summary>
		/// <remarks>
		/// Modifies the given <paramref name="previousMap"/> to perform the rematching.
		/// </remarks>
		/// <returns>Return <see langword="false"/> when previous mapping cannot be used or repaired.</returns>
		public static bool TryRematchMatchedSpools(this IMachineState state, Dictionary<int, MaterialToPrint> materialsToPrint, Dictionary<int, SpoolMatch> previousMap)
		{
			// Collect filament ids whose previously-mapped spool no longer matches the current machine state.

			var invalidFilamentIds = new HashSet<int>();

			foreach (var (filamentId, previousMatch) in previousMap)
			{
				// Check if the current state still contains the matched spool material from the previous mapping.

				if (!state.TryGetLoadedSpool(previousMatch.Location, out var currentMaterial)
					|| !previousMatch.MaterialInMatchedSpool.IsSimilar(currentMaterial.Value))
				{
					// Filament isn't loaded on that location or material/color doesn't match anymore.

					invalidFilamentIds.Add(filamentId);
					continue;
				}
			}

			if (invalidFilamentIds.Count == 0) return true;

			// Build the subset of materials that need rematching.
			var materialsToRematch = materialsToPrint.Where(kv => invalidFilamentIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

			// Find available spools for the materials that need rematching.
			var rematchResults = state.FindMatchingSpools(materialsToRematch);

			var confirmedReplacements = new Dictionary<int, SpoolMatch>();

			foreach (var filamentId in materialsToRematch.Keys)
			{
				if (!rematchResults.All.TryGetValue(filamentId, out var similarMatches))
				{
					return false;
				}

				if (similarMatches.Count > 0)
				{
					confirmedReplacements[filamentId] = similarMatches.First();
				}
				else
				{
					// Unable to rematch with the same material and similar color.
					return false;
				}
			}

			foreach (var (filamentId, newMatch) in confirmedReplacements)
			{
				previousMap[filamentId] = newMatch;
			}

			return true;
		}

		public static bool TryGetLoadedSpool(this IMachineState state, SpoolLocation location, [NotNullWhen(true)] out Material? loadedMaterial)
		{
			loadedMaterial = null;

			foreach (var mu in state.MaterialUnits)
			{
				foreach (var (_, spool) in mu.Trays)
				{
					if (mu.ID.Equals(location.MUID) && spool.Number == location.Slot)
					{
						loadedMaterial = spool.Material;

						return true;
					}

				}
			}
			return false;
		}

		public static void EnsureSupportPrintOptions(this IMachineState state, PrintOptions options)
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

		public static bool HasFeature(this IMachineState state, MachineCapabilities desiredFeature)
			=> state.Capabilities.HasFlag(desiredFeature);

		public static bool IfNotCapable(this IMachineState state, MachineCapabilities desiredFeature, [NotNullWhen(true)] out MachineOperationResult? operationResult)
		{
			if (state.HasFeature(desiredFeature))
			{
				operationResult = null;
				return false;
			}

			operationResult = MachineOperationResult.Fail(MachineMessages.NoFeature(desiredFeature));
			return true;
		}
	}
}
