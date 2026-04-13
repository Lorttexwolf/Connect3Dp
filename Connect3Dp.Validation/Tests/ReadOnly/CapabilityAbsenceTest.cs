using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class CapabilityAbsenceTest : ValidationTest
{
	public override string Name => "Capability Absence";
	public override string Description => "Verify capabilities that should NOT be present are absent";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var actual = connection.State.Capabilities;
		var shouldBeAbsent = spec.ExplicitlyAbsentCapabilities;
		var unexpected = shouldBeAbsent & actual;

		if (unexpected == 0)
			return Task.FromResult(TestResult.Pass("No unexpected capabilities found"));

		var unexpectedNames = new List<string>();
		foreach (MachineCapabilities cap in Enum.GetValues<MachineCapabilities>())
		{
			if (cap != MachineCapabilities.None && unexpected.HasFlag(cap))
				unexpectedNames.Add(cap.ToString());
		}

		return Task.FromResult(TestResult.Fail(
			$"{unexpectedNames.Count} capabilities present that should be absent",
			string.Join(", ", unexpectedNames)));
	}
}
