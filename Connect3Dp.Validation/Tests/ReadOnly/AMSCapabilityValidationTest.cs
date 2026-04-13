using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab.Constants;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class AMSCapabilityValidationTest : ValidationTest
{
	public override string Name => "AMS Capability Validation";
	public override string Description => "Validate each AMS unit's capabilities match its model spec";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedAMSModel == null)
			return Task.FromResult(TestResult.Skip("Standalone configuration — AMS validation skipped"));

		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Fail($"Expected {spec.ExpectedAMSModel} but no AMS units detected"));

		var failures = new List<string>();
		int validated = 0;

		// Verify at least one unit matches the expected AMS model
		if (!units.Any(u => u.Model == spec.ExpectedAMSModel))
		{
			var found = string.Join(", ", units.Select(u => u.Model ?? "Unknown"));
			failures.Add($"No unit matches expected model {spec.ExpectedAMSModel} (found: {found})");
		}

		foreach (var unit in units)
		{
			if (unit.Model == null)
			{
				failures.Add($"Unit {unit.ID}: model is null");
				continue;
			}

			validated++;

			var expectedCaps = BBLConstants.GetAMSFeaturesFromModel(unit.Model);
			if (unit.Capabilities != expectedCaps)
				failures.Add($"{unit.Model} ({unit.ID}): capabilities {unit.Capabilities}, expected {expectedCaps}");

			var expectedCapacity = BBLConstants.GetAMSCapacityFromModel(unit.Model);
			if (unit.Capacity != expectedCapacity)
				failures.Add($"{unit.Model} ({unit.ID}): capacity {unit.Capacity}, expected {expectedCapacity}");

			var expectedHeating = BBLConstants.GetAMSHeatingConstraintsFromModel(unit.Model);
			if (unit.HeatingConstraints != expectedHeating)
				failures.Add($"{unit.Model} ({unit.ID}): heating {unit.HeatingConstraints}, expected {expectedHeating}");
		}

		if (failures.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{failures.Count} AMS validation failures",
				string.Join("; ", failures)));

		return Task.FromResult(TestResult.Pass($"All {validated} AMS unit(s) match their model spec"));
	}
}
