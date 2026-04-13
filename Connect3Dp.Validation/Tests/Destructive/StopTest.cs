using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class StopTest : ValidationTest
{
	public override string Name => "Stop";
	public override string Description => "Stop the current print job";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.Control))
			return TestResult.Skip("Control capability not present");

		if (connection.State.Status is not (MachineStatus.Printing or MachineStatus.Paused))
			return TestResult.Fail("Printer is not printing or paused", $"Status: {connection.State.Status}");

		var result = await connection.Stop();

		if (!result.Success)
			return TestResult.Fail("Stop failed", result.Reasoning?.ToString());

		return connection.State.Status == MachineStatus.Canceled
			? TestResult.Pass("Print stopped successfully")
			: TestResult.Fail($"Status after stop: {connection.State.Status}, expected Canceled");
	}
}
