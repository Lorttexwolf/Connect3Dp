using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class AirDuctToggleTest : ValidationTest
{
	public override string Name => "Air Duct Toggle";
	public override string Description => "Toggle air duct mode (P2S only) or verify absence";
	public override RiskTier Tier => RiskTier.NonDestructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		bool shouldHaveAirDuct = spec.ExpectedCapabilities.HasFlag(MachineCapabilities.AirDuct);
		bool hasAirDuct = connection.State.Capabilities.HasFlag(MachineCapabilities.AirDuct);

		if (!shouldHaveAirDuct)
		{
			// Validate it's correctly absent
			return hasAirDuct
				? TestResult.Fail("AirDuct capability present but should be absent for this model")
				: TestResult.Pass("AirDuct correctly absent for this model");
		}

		// Model should have AirDuct — test toggling
		if (!hasAirDuct)
			return TestResult.Fail("AirDuct capability expected but not present");

		var originalMode = connection.State.AirDuctMode;
		var targetMode = originalMode == MachineAirDuctMode.Cooling
			? MachineAirDuctMode.Heating
			: MachineAirDuctMode.Cooling;

		var result = await connection.ChangeAirDuct(targetMode);
		if (!result.Success)
			return TestResult.Fail("ChangeAirDuct failed", result.Reasoning?.ToString());

		// Verify state changed
		if (connection.State.AirDuctMode != targetMode)
		{
			return TestResult.Fail($"Air duct mode did not change to {targetMode}");
		}

		// Restore
		await connection.ChangeAirDuct(originalMode);

		return TestResult.Pass($"Air duct toggled to {targetMode} and restored");
	}
}
