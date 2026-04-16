using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class LightToggleTest : ValidationTest
{
	public override string Name => "Light Toggle";
	public override string Description => "Toggle chamber light on/off and verify state change";
	public override RiskTier Tier => RiskTier.NonDestructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.Lighting))
			return TestResult.Skip("Lighting not present");

		bool currentState = connection.State.Lights.TryGetValue("Chamber", out var isOn) && isOn;
		bool targetState = !currentState;

		var result = await connection.ToggleLight("Chamber", targetState);
		if (!result.Success)
			return TestResult.Fail("ToggleLight failed", result.Reasoning?.ToString());

		if (!connection.State.Lights.TryGetValue("Chamber", out var newState) || newState != targetState)
		{
			await connection.ToggleLight("Chamber", currentState);
			return TestResult.Fail($"State did not change to {(targetState ? "ON" : "OFF")}");
		}

		await connection.ToggleLight("Chamber", currentState);

		return TestResult.Pass("Toggled and restored");
	}
}
