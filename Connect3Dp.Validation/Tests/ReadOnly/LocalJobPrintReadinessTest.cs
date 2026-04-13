using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class LocalJobPrintReadinessTest : ValidationTest
{
	public override string Name => "Local Job Print Readiness";
	public override string Description => "Verify the validation print file has valid metadata for printing";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return Task.FromResult(TestResult.Skip("LocalJobs capability not present"));

		if (string.IsNullOrEmpty(spec.ValidationPrintFileName))
			return Task.FromResult(TestResult.Skip("No validation print file defined for this model"));

		var job = connection.State.LocalJobs.FirstOrDefault(j =>
			j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

		if (job.Name == null)
			return Task.FromResult(TestResult.Skip($"Validation file '{spec.ValidationPrintFileName}' not found on printer"));

		var issues = new List<string>();

		if (job.Time <= TimeSpan.Zero)
			issues.Add("Time estimate is zero or negative");

		if (job.TotalGramsUsed <= 0)
			issues.Add("TotalGramsUsed is zero or negative");

		if (job.MaterialsToPrint == null || job.MaterialsToPrint.Count == 0)
			issues.Add("MaterialsToPrint is empty");

		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob))
			issues.Add("StartLocalJob capability is absent — cannot start this file remotely");

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"Validation file has {issues.Count} readiness issue(s)",
				string.Join("; ", issues)));

		return Task.FromResult(TestResult.Pass(
			$"'{job.Name}' is ready: {job.Time.TotalMinutes:F0} min, {job.TotalGramsUsed}g, {job.MaterialsToPrint!.Count} material(s)"));
	}
}
