using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class LocalJobsTest : ValidationTest
{
	public override string Name => "Local Jobs";
	public override string Description => "Verify local jobs are discoverable and the validation print file is present";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return Task.FromResult(TestResult.Skip("LocalJobs capability not present (FTP may not be connected)"));

		var jobs = connection.State.LocalJobs;

		if (jobs.Count == 0)
			return Task.FromResult(TestResult.Skip("LocalJobs capability present but no files found on device"));

		// Validate each job has basic required data
		var invalid = jobs.Where(j => string.IsNullOrWhiteSpace(j.Name)).ToList();
		if (invalid.Count > 0)
			return Task.FromResult(TestResult.Fail($"{invalid.Count} job(s) have empty names"));

		// Check for the validation print file if one is defined in the spec
		if (!string.IsNullOrEmpty(spec.ValidationPrintFileName))
		{
			var validationJob = jobs.FirstOrDefault(j =>
				j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

			if (validationJob.Name == null)
			{
				var jobNames = string.Join(", ", jobs.Select(j => j.Name).Take(10));
				return Task.FromResult(TestResult.Fail(
					$"Validation file '{spec.ValidationPrintFileName}' not found on printer",
					$"Found {jobs.Count} file(s): {jobNames}"));
			}

			return Task.FromResult(TestResult.Pass(
				$"Found validation file '{validationJob.Name}' among {jobs.Count} local job(s)"));
		}

		var names = string.Join(", ", jobs.Select(j => j.Name).Take(5));
		return Task.FromResult(TestResult.Pass(
			$"{jobs.Count} local job(s) found: {names}{(jobs.Count > 5 ? "..." : "")}"));
	}
}
