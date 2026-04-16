using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class LocalJobsTest : ValidationTest
{
	public override string Name => "Local Storage File Discovery";
	public override string Description => "Verify local jobs are discoverable and the validation print file is present";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return TestResult.Skip("LocalJobs not present");

		// Wait for FTP scan to fully complete
		while (connection.State.IsLocalStorageScanning)
			await Task.Delay(500, ct);

		var jobs = connection.State.LocalJobs;

		if (jobs.Count == 0)
			return TestResult.Skip("LocalJobs capability present but no files found on device");

		// Validate each job has basic required data
		var invalid = jobs.Where(j => string.IsNullOrWhiteSpace(j.Name)).ToList();
		if (invalid.Count > 0)
			return TestResult.Fail($"{invalid.Count} job(s) have empty names");

		// Check for the validation print file if one is defined in the spec
		if (!string.IsNullOrEmpty(spec.ValidationPrintFileName))
		{
			var validationJob = jobs.FirstOrDefault(j =>
				j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

			if (validationJob.Name == null)
			{
				return TestResult.Fail(
					$"Validation file '{spec.ValidationPrintFileName}' not found among {jobs.Count} file(s) on printer");
			}

			return TestResult.Pass(
				$"Found '{validationJob.Name}' ({jobs.Count} file(s) total)");
		}

		return TestResult.Pass($"{jobs.Count} file(s) found");
	}
}
