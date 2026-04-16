using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class AMSHeatingToggleTest : ValidationTest
{
	public override string Name => "AMS Heating Toggle";
	public override string Description => "Start and stop AMS heating on each heating-capable unit";
	public override RiskTier Tier => RiskTier.NonDestructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var heatingUnits = connection.State.MaterialUnits
			.Where(u => u.Capabilities.HasFlag(MUCapabilities.Heating) && u.HeatingConstraints.HasValue)
			.ToList();

		if (heatingUnits.Count == 0)
			return TestResult.Skip("No heating-capable AMS units detected");

		var results = new List<string>();

		foreach (var heatingUnit in heatingUnits)
		{
			// Don't interfere with an active heating job
			if (heatingUnit.HeatingJob.HasValue)
			{
				results.Add($"{heatingUnit.Model ?? "Unknown"} ({heatingUnit.ID}): skipped (already heating)");
				continue;
			}

			// Use minimum safe temperature and short duration
			var constraints = heatingUnit.HeatingConstraints!.Value;
			bool canSpin = heatingUnit.Capabilities.HasFlag(MUCapabilities.Heating_CanSpin);
			var settings = new HeatingSettings(constraints.MinTempC, TimeSpan.FromMinutes(5), canSpin ? false : null);

			// Begin heating
			var beginResult = await connection.BeginMUHeating(heatingUnit.ID, settings);

			if (!beginResult.Success)
				return TestResult.Fail($"BeginMUHeating failed on {heatingUnit.ID}", beginResult.Reasoning?.ToString());

			// Wait for state to update
			await Task.Delay(10_000, ct);

			// Verify heating job appeared
			var unitAfterBegin = connection.State.MaterialUnits.FirstOrDefault(u => u.ID == heatingUnit.ID);

			if (unitAfterBegin == null || !unitAfterBegin.HeatingJob.HasValue)
			{
				await connection.EndMUHeating(heatingUnit.ID);
				return TestResult.Fail($"HeatingJob did not appear on {heatingUnit.ID} after BeginMUHeating");
			}

			// End heating to restore state
			var endResult = await connection.EndMUHeating(heatingUnit.ID);

			if (!endResult.Success)
				return TestResult.Fail($"EndMUHeating failed on {heatingUnit.ID} — heating may still be active!", endResult.Reasoning?.ToString());

			// Wait for state to update
			await Task.Delay(10_000, ct);

			// Verify heating job cleared
			var unitAfterEnd = connection.State.MaterialUnits.FirstOrDefault(u => u.ID == heatingUnit.ID);

			if (unitAfterEnd != null && unitAfterEnd.HeatingJob.HasValue)
				return TestResult.Fail($"HeatingJob still present on {heatingUnit.ID} after EndMUHeating");

			results.Add($"{heatingUnit.Model ?? "Unknown"} ({heatingUnit.ID}): toggled at {constraints.MinTempC}°C");
		}

		return TestResult.Pass($"Toggled on {results.Count} unit(s)");
	}
}
