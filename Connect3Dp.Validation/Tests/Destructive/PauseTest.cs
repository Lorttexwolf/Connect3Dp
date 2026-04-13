using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class PauseTest : ValidationTest
{
	public override string Name => "Pause";
	public override string Description => "Pause the current print job";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.Control))
			return TestResult.Skip("Control capability not present");

		if (connection.State.Status != MachineStatus.Printing)
			return TestResult.Fail("Printer is not currently printing", $"Status: {connection.State.Status}");

		var result = await connection.Pause();

		if (!result.Success)
			return TestResult.Fail("Pause failed", result.Reasoning?.ToString());

		return connection.State.Status == MachineStatus.Paused
			? TestResult.Pass("Print paused successfully")
			: TestResult.Fail($"Status after pause: {connection.State.Status}, expected Paused");
	}
}
