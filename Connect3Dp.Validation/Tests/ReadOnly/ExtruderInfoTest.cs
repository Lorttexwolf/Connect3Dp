using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class ExtruderInfoTest : ValidationTest
{
	public override string Name => "Extruder Info";
	public override string Description => "Verify extruder data is reported with valid temperatures and constraints";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var extruders = connection.State.Extruders.ToList();

		if (extruders.Count == 0)
			return Task.FromResult(TestResult.Skip("No extruder data"));

		var issues = new List<string>();

		foreach (var ext in extruders)
		{
			if (ext.HeatingConstraint.MinTempC >= ext.HeatingConstraint.MaxTempC)
				issues.Add($"Extruder {ext.Number} has invalid constraints: min {ext.HeatingConstraint.MinTempC} >= max {ext.HeatingConstraint.MaxTempC}");

			if (ext.TempC < 0)
				issues.Add($"Extruder {ext.Number} has negative temperature: {ext.TempC}°C");

			if (ext.TargetTempC.HasValue && ext.TargetTempC.Value > 0)
			{
				if (ext.TargetTempC.Value < ext.HeatingConstraint.MinTempC || ext.TargetTempC.Value > ext.HeatingConstraint.MaxTempC)
					issues.Add($"Extruder {ext.Number} target {ext.TargetTempC.Value}°C is outside constraints [{ext.HeatingConstraint.MinTempC}–{ext.HeatingConstraint.MaxTempC}]");
			}
		}

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{issues.Count} extruder issue(s)",
				string.Join("; ", issues)));

		var details = extruders.Select(e =>
			$"#{e.Number}: {e.TempC:F1}°C" + (e.TargetTempC.HasValue ? $" → {e.TargetTempC.Value:F1}°C" : ""));

		return Task.FromResult(TestResult.Pass(
			$"{extruders.Count} extruder(s): {string.Join(", ", details)}"));
	}
}
