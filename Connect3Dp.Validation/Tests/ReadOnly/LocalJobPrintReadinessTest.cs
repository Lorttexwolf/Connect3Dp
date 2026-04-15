using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Extensions;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class LocalJobPrintReadinessTest : ValidationTest
{
	public override string Name => "Local Job Print Readiness";
	public override string Description => "Verify the validation print file has valid metadata for printing";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return TestResult.Skip("LocalJobs not present");

		while (connection.State.IsLocalStorageScanning)
			await Task.Delay(500, ct);

		if (string.IsNullOrEmpty(spec.ValidationPrintFileName))
			return TestResult.Skip("No validation print file in spec");

		var job = connection.State.LocalJobs.FirstOrDefault(j =>
			j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

		if (job.Name == null)
			return TestResult.Skip($"'{spec.ValidationPrintFileName}' not found on printer");

		var issues = new List<string>();

		if (job.Time <= TimeSpan.Zero)
			issues.Add("Time estimate <= 0");

		if (job.TotalGramsUsed <= 0)
			issues.Add("TotalGramsUsed <= 0");

		if (job.MaterialsToPrint == null || job.MaterialsToPrint.Count == 0)
			issues.Add("MaterialsToPrint empty");

		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob))
			issues.Add("StartLocalJob absent");

		if (issues.Count > 0)
			return TestResult.Fail(
				$"{issues.Count} readiness issue(s)",
				string.Join("; ", issues));

		var matches = connection.State.FindMatchingSpools(job.MaterialsToPrint);
		if (matches.HasMissing)
			return TestResult.Fail($"{matches.Missing.Count} material(s) could not be matched to a loaded slot");

		return TestResult.Pass(
			$"'{job.Name}' ready: {job.Time.TotalMinutes:F0} min, {job.TotalGramsUsed}g, {job.MaterialsToPrint!.Count} material(s), all matched");
	}
}
