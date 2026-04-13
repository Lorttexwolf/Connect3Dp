using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class MarkAsIdleTest : ValidationTest
{
	public override string Name => "Mark as Idle";
	public override string Description => "Clear the bed and return to idle state";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (connection.State.Status is not (MachineStatus.Printed or MachineStatus.Canceled))
			return TestResult.Fail("Printer is not in Printed or Canceled state", $"Status: {connection.State.Status}");

		var result = await connection.MarkAsIdle();

		if (!result.Success)
			return TestResult.Fail("MarkAsIdle failed", result.Reasoning?.ToString());

		return connection.State.Status == MachineStatus.Idle
			? TestResult.Pass("Printer marked as idle successfully")
			: TestResult.Fail($"Status after MarkAsIdle: {connection.State.Status}, expected Idle");
	}
}
