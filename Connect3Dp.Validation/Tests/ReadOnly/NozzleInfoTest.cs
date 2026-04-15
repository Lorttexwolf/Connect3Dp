using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class NozzleInfoTest : ValidationTest
{
	public override string Name => "Nozzle Info";
	public override string Description => "Verify nozzle count and diameter match expected configuration";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var nozzles = connection.State.Nozzles.ToList();

		if (nozzles.Count == 0)
			return Task.FromResult(TestResult.Skip("No nozzle data"));

		var issues = new List<string>();

		// Check nozzle count
		if (nozzles.Count != spec.ExpectedNozzleCount)
			issues.Add($"Expected {spec.ExpectedNozzleCount} nozzle(s) but found {nozzles.Count}");

		// Check each nozzle's diameter
		foreach (var nozzle in nozzles)
		{
			if (nozzle.Diameter <= 0)
			{
				issues.Add($"Nozzle {nozzle.Number} has invalid diameter: {nozzle.Diameter}mm");
			}
			else if (Math.Abs(nozzle.Diameter - spec.DefaultNozzleDiameter) > 0.01)
			{
				issues.Add($"Nozzle {nozzle.Number} diameter {nozzle.Diameter}mm does not match expected {spec.DefaultNozzleDiameter}mm");
			}
		}

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{issues.Count} nozzle issue(s)",
				string.Join("; ", issues)));

		var details = nozzles.Select(n => $"#{n.Number}: {n.Diameter}mm");
		return Task.FromResult(TestResult.Pass(
			$"{nozzles.Count} nozzle(s): {string.Join(", ", details)}"));
	}
}
