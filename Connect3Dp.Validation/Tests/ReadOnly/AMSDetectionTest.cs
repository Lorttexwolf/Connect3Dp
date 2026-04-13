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
		if (spec.Brand != PrinterBrand.BambuLab)
			return Task.FromResult(TestResult.Skip("AMS detection only available for BambuLab"));

		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Skip("No AMS units detected (none attached)"));

		var details = units.Select(u =>
			$"{u.Model ?? "Unknown"} (ID: {u.ID}, Capacity: {u.Capacity}, Caps: {u.Capabilities})"
		);

		return Task.FromResult(TestResult.Pass(
			$"{units.Count} AMS unit(s) detected: {string.Join("; ", details)}"));
	}
}
