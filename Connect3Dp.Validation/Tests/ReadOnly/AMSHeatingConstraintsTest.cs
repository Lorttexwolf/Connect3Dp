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
		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Skip("No AMS detected"));

		var heatingUnits = units.Where(u => u.Capabilities.HasFlag(MUCapabilities.Heating)).ToList();

		if (heatingUnits.Count == 0)
			return Task.FromResult(TestResult.Skip("No heating-capable units"));

		var issues = new List<string>();

		foreach (var unit in heatingUnits)
		{
			if (!unit.HeatingConstraints.HasValue)
			{
				issues.Add($"{unit.ID}: has Heating but no constraints");
				continue;
			}

			var constraints = unit.HeatingConstraints.Value;

			if (constraints.MinTempC >= constraints.MaxTempC)
				issues.Add($"{unit.ID}: min {constraints.MinTempC}°C >= max {constraints.MaxTempC}°C");

			if (constraints.MinTempC <= 0)
				issues.Add($"{unit.ID}: min temp <= 0");

			if (unit.Model != null)
			{
				var expected = BBLConstants.GetAMSHeatingConstraintsFromModel(unit.Model);
				if (expected.HasValue && constraints != expected.Value)
					issues.Add($"{unit.ID}: {constraints}, expected {expected.Value}");
			}
		}

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{issues.Count} heating constraint issue(s)",
				string.Join("; ", issues)));

		return Task.FromResult(TestResult.Pass($"{heatingUnits.Count} unit(s) validated"));
	}
}
