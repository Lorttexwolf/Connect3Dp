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
		bool shouldHaveLighting = spec.ExpectedCapabilities.HasFlag(MachineCapabilities.Lighting);
		bool hasLighting = connection.State.Capabilities.HasFlag(MachineCapabilities.Lighting);

		if (!shouldHaveLighting)
		{
			return hasLighting
				? TestResult.Fail("Lighting capability present but should be absent")
				: TestResult.Pass("Lighting correctly absent for this model");
		}

		if (!hasLighting)
			return TestResult.Fail("Lighting capability expected but not present");

		// Read current light state
		bool currentState = connection.State.Lights.TryGetValue("Chamber", out var isOn) && isOn;
		bool targetState = !currentState;

		// Toggle to opposite
		var result = await connection.ToggleLight("Chamber", targetState);
		if (!result.Success)
			return TestResult.Fail("ToggleLight failed", result.Reasoning?.ToString());

		// Verify state changed
		if (!connection.State.Lights.TryGetValue("Chamber", out var newState) || newState != targetState)
		{
			// Restore just in case
			await connection.ToggleLight("Chamber", currentState);
			return TestResult.Fail($"Light state did not change to {(targetState ? "ON" : "OFF")}");
		}

		// Restore original state
		await connection.ToggleLight("Chamber", currentState);

		return TestResult.Pass($"Light toggled {(targetState ? "ON" : "OFF")} and restored");
	}
}
