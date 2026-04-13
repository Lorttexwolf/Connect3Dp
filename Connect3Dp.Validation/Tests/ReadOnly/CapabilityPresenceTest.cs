using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;
using System.Text;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class CapabilityPresenceTest : ValidationTest
{
	public override string Name => "Capability Presence";
	public override string Description => "Verify all expected capabilities are present";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var actual = connection.State.Capabilities;
		var expected = spec.ExpectedCapabilities;
		var missing = expected & ~actual;

		if (missing == 0)
			return Task.FromResult(TestResult.Pass($"All expected capabilities present"));

		var missingNames = new List<string>();
		foreach (MachineCapabilities cap in Enum.GetValues<MachineCapabilities>())
		{
			if (cap != MachineCapabilities.None && missing.HasFlag(cap))
				missingNames.Add(cap.ToString());
		}

		return Task.FromResult(TestResult.Fail(
			$"Missing {missingNames.Count} expected capabilities",
			string.Join(", ", missingNames)));
	}
}
