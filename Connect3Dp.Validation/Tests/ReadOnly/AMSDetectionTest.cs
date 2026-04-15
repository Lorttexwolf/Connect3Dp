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
		var units = connection.State.MaterialUnits.ToList();

		if (units.Count == 0)
			return Task.FromResult(TestResult.Skip("No AMS detected"));

		return Task.FromResult(TestResult.Pass($"{units.Count} unit(s) detected"));
	}
}
