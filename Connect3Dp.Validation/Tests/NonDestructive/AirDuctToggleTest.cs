using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class AirDuctToggleTest : ValidationTest
{
	public override string Name => "Air Duct Toggle";
	public override string Description => "Toggle air duct mode and verify state change";
	public override RiskTier Tier => RiskTier.NonDestructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.AirDuct))
			return TestResult.Skip("AirDuct not present");

		var originalMode = connection.State.AirDuctMode;
		var targetMode = originalMode == MachineAirDuctMode.Cooling
			? MachineAirDuctMode.Heating
			: MachineAirDuctMode.Cooling;

		var result = await connection.ChangeAirDuct(targetMode);
		if (!result.Success)
			return TestResult.Fail("ChangeAirDuct failed", result.Reasoning?.ToString());

		if (connection.State.AirDuctMode != targetMode)
			return TestResult.Fail($"Mode did not change to {targetMode}");

		await connection.ChangeAirDuct(originalMode);

		return TestResult.Pass("Toggled and restored");
	}
}
