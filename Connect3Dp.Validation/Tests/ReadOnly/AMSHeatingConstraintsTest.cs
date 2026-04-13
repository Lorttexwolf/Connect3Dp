using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab.Constants;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class AMSHeatingConstraintsTest : ValidationTest
{
	public override string Name => "AMS Heating Constraints";
	public override string Description => "Verify AMS units with heating capability have valid temperature constraints";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedAMSModel == null)
			return Task.FromResult(TestResult.Skip("Standalone configuration — AMS heating validation skipped"));

		var expectedHeating = BBLConstants.GetAMSHeatingConstraintsFromModel(spec.ExpectedAMSModel);
		if (expectedHeating == null)
			return Task.FromResult(TestResult.Skip($"{spec.ExpectedAMSModel} does not support heating"));

		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Fail($"Expected {spec.ExpectedAMSModel} but no AMS units detected"));

		var heatingUnits = units.Where(u => u.Capabilities.HasFlag(MUCapabilities.Heating)).ToList();

		if (heatingUnits.Count == 0)
			return Task.FromResult(TestResult.Fail(
				$"Expected heating-capable {spec.ExpectedAMSModel} but no units have Heating capability"));

		var issues = new List<string>();

		foreach (var unit in heatingUnits)
		{
			if (!unit.HeatingConstraints.HasValue)
			{
				issues.Add($"{unit.Model ?? "Unknown"} (ID: {unit.ID}) has Heating capability but no HeatingConstraints");
				continue;
			}

			var constraints = unit.HeatingConstraints.Value;

			if (constraints.MinTempC >= constraints.MaxTempC)
				issues.Add($"{unit.Model ?? "Unknown"} (ID: {unit.ID}) has invalid range: min {constraints.MinTempC}°C >= max {constraints.MaxTempC}°C");

			if (constraints.MinTempC <= 0)
				issues.Add($"{unit.Model ?? "Unknown"} (ID: {unit.ID}) has non-positive min temp: {constraints.MinTempC}°C");
		}

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{issues.Count} heating constraint issue(s)",
				string.Join("; ", issues)));

		var details = heatingUnits.Select(u =>
			$"{u.Model ?? "Unknown"} (ID: {u.ID}): {u.HeatingConstraints!.Value.MinTempC}–{u.HeatingConstraints!.Value.MaxTempC}°C");

		return Task.FromResult(TestResult.Pass(
			$"{heatingUnits.Count} heating-capable unit(s) validated: {string.Join("; ", details)}"));
	}
}
