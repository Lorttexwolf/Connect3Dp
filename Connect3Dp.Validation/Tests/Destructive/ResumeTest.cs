using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class ResumeTest : ValidationTest
{
	public override string Name => "Resume";
	public override string Description => "Resume the paused print job";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.Control))
			return TestResult.Skip("Control capability not present");

		if (connection.State.Status != MachineStatus.Paused)
			return TestResult.Fail("Printer is not paused", $"Status: {connection.State.Status}");

		var result = await connection.Resume();

		if (!result.Success)
			return TestResult.Fail("Resume failed", result.Reasoning?.ToString());

		return connection.State.Status == MachineStatus.Printing
			? TestResult.Pass("Print resumed successfully")
			: TestResult.Fail($"Status after resume: {connection.State.Status}, expected Printing");
	}
}
