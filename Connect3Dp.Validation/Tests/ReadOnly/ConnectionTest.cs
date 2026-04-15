using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class ConnectionTest : ValidationTest
{
	public override string Name => "Connection";
	public override string Description => "Connect to the printer and verify it responds";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		var result = await connection.ConnectIfDisconnected(ct);

		if (!result.Success)
			return TestResult.Fail("Failed to connect", result.Reasoning?.ToString());

		// Wait briefly for state to populate
		var timeout = TimeSpan.FromSeconds(10);
		var sw = System.Diagnostics.Stopwatch.StartNew();

		while (sw.Elapsed < timeout && connection.State.Status == MachineStatus.Connecting)
			await Task.Delay(500, ct);

		if (connection.State.Status == MachineStatus.Disconnected)
			return TestResult.Fail("Connected but status reverted to Disconnected");

		return TestResult.Pass($"Status: {connection.State.Status}");
	}
}
