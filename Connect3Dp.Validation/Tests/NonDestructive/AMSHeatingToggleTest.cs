using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class AMSHeatingToggleTest : ValidationTest
{
	public override string Name => "AMS Heating Toggle";
	public override string Description => "Start and stop AMS heating to verify the full heating pipeline";
	public override RiskTier Tier => RiskTier.NonDestructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedAMSModel == null)
			return TestResult.Skip("Standalone configuration — AMS heating skipped");

		var expectedHeating = BBLConstants.GetAMSHeatingConstraintsFromModel(spec.ExpectedAMSModel);
		if (expectedHeating == null)
			return TestResult.Skip($"{spec.ExpectedAMSModel} does not support heating");

		var units = connection.State.MaterialUnits.ToList();
		var heatingUnit = units.FirstOrDefault(u =>
			u.Capabilities.HasFlag(MUCapabilities.Heating) && u.HeatingConstraints.HasValue);

		if (heatingUnit == null)
			return TestResult.Fail($"Expected heating-capable {spec.ExpectedAMSModel} but no units with Heating capability found");

		// Don't interfere with an active heating job
		if (heatingUnit.HeatingJob.HasValue)
			return TestResult.Skip($"AMS unit '{heatingUnit.ID}' is already heating — skipping to avoid interference");

		// Use minimum safe temperature and short duration
		var constraints = heatingUnit.HeatingConstraints!.Value;
		bool canSpin = heatingUnit.Capabilities.HasFlag(MUCapabilities.Heating_CanSpin);
		var settings = new HeatingSettings(constraints.MinTempC, TimeSpan.FromMinutes(5), canSpin ? false : null);

		// Begin heating
		var beginResult = await connection.BeginMUHeating(heatingUnit.ID, settings);

		if (!beginResult.Success)
			return TestResult.Fail("BeginMUHeating failed", beginResult.Reasoning?.ToString());

		// Verify heating job appeared
		var unitAfterBegin = connection.State.MaterialUnits.FirstOrDefault(u => u.ID == heatingUnit.ID);

		if (unitAfterBegin == null || !unitAfterBegin.HeatingJob.HasValue)
		{
			// Try to clean up
			await connection.EndMUHeating(heatingUnit.ID);
			return TestResult.Fail("HeatingJob did not appear after BeginMUHeating");
		}

		// End heating to restore state
		var endResult = await connection.EndMUHeating(heatingUnit.ID);

		if (!endResult.Success)
			return TestResult.Fail("EndMUHeating failed — heating may still be active!", endResult.Reasoning?.ToString());

		// Verify heating job cleared
		var unitAfterEnd = connection.State.MaterialUnits.FirstOrDefault(u => u.ID == heatingUnit.ID);

		if (unitAfterEnd != null && unitAfterEnd.HeatingJob.HasValue)
			return TestResult.Fail("HeatingJob still present after EndMUHeating");

		return TestResult.Pass(
			$"Heating toggled on {heatingUnit.Model ?? "Unknown"} (ID: {heatingUnit.ID}) at {constraints.MinTempC}°C and restored");
	}
}
