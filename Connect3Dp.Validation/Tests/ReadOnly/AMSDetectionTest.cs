using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class AMSDetectionTest : ValidationTest
{
	public override string Name => "AMS Detection";
	public override string Description => "Detect and report attached material units (AMS)";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedAMSModel == null)
			return Task.FromResult(TestResult.Skip("Standalone configuration — AMS detection skipped"));

		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Fail($"Expected {spec.ExpectedAMSModel} but no AMS units detected"));

		var matching = units.Where(u => u.Model == spec.ExpectedAMSModel).ToList();

		if (matching.Count == 0)
		{
			var found = string.Join(", ", units.Select(u => u.Model ?? "Unknown"));
			return Task.FromResult(TestResult.Fail(
				$"Expected {spec.ExpectedAMSModel} but found: {found}"));
		}

		var details = units.Select(u =>
			$"{u.Model ?? "Unknown"} (ID: {u.ID}, Capacity: {u.Capacity}, Caps: {u.Capabilities})"
		);

		return Task.FromResult(TestResult.Pass(
			$"{units.Count} AMS unit(s) detected, {matching.Count} matching {spec.ExpectedAMSModel}: {string.Join("; ", details)}"));
	}
}
