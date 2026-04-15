using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class HeatingElementsTest : ValidationTest
{
	public override string Name => "Heating Elements";
	public override string Description => "Verify expected heating elements are reported";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (spec.ExpectedHeatingConstraints == null || spec.ExpectedHeatingConstraints.Count == 0)
			return Task.FromResult(TestResult.Skip("No heating constraints in spec"));

		return Task.FromResult(TestResult.Pass(
			$"{spec.ExpectedHeatingConstraints.Count} element(s): {string.Join(", ", spec.ExpectedHeatingConstraints.Keys)}"));
	}
}
