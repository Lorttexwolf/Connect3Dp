using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class PrintLocalFileTest : ValidationTest
{
	public override string Name => "Print Local File";
	public override string Description => "Start the validation print file to verify the full print-start pipeline";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob))
			return TestResult.Skip("StartLocalJob capability not present");

		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return TestResult.Skip("LocalJobs capability not present (FTP may not be connected)");

		if (string.IsNullOrEmpty(spec.ValidationPrintFileName))
			return TestResult.Skip("No validation print file defined for this model");

		if (connection.State.Status != MachineStatus.Idle)
			return TestResult.Fail("Printer is not idle", $"Status: {connection.State.Status}");

		// Find the validation file
		var job = connection.State.LocalJobs.FirstOrDefault(j =>
			j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

		if (job.Name == null)
			return TestResult.Fail(
				$"Validation file '{spec.ValidationPrintFileName}' not found on printer",
				"Please load the file onto the printer via USB, SD card, or slicer software");

		// Use safe print options — no calibration, no inspection
		var options = new PrintOptions(
			CustomID: null,
			LevelBed: false,
			FlowCalibration: false,
			VibrationCalibration: false,
			InspectFirstLayer: false,
			MaterialMap: null);

		var result = await connection.PrintLocal(job, options);

		if (!result.Success)
			return TestResult.Fail("PrintLocal failed", result.Reasoning?.ToString());

		return connection.State.Status == MachineStatus.Printing
			? TestResult.Pass($"Print started: '{job.Name}'")
			: TestResult.Fail($"Status after PrintLocal: {connection.State.Status}, expected Printing");
	}
}
