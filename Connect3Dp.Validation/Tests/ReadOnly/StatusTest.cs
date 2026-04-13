using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class StatusTest : ValidationTest
{
	public override string Name => "Status";
	public override string Description => "Verify printer reports a valid operational status";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var status = connection.State.Status;

		if (status == MachineStatus.Disconnected)
			return Task.FromResult(TestResult.Fail("Printer reports Disconnected"));

		if (status == MachineStatus.Connecting)
			return Task.FromResult(TestResult.Fail("Printer is still Connecting — state not fully loaded"));

		return Task.FromResult(TestResult.Pass($"Status: {status}"));
	}
}
