using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class ActiveJobInfoTest : ValidationTest
{
	public override string Name => "Active Job Info";
	public override string Description => "Verify active print job reports valid metadata while printing";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (connection.State.Status is not (MachineStatus.Printing or MachineStatus.Paused))
			return Task.FromResult(TestResult.Skip($"Printer is not printing (status: {connection.State.Status})"));

		var job = connection.State.Job;

		if (job == null)
			return Task.FromResult(TestResult.Fail("Status is Printing/Paused but State.Job is null"));

		var issues = new List<string>();

		if (string.IsNullOrWhiteSpace(job.Name))
			issues.Add("Job name is empty");

		if (job.PercentageComplete < 0 || job.PercentageComplete > 100)
			issues.Add($"PercentageComplete out of range: {job.PercentageComplete}%");

		if (job.TotalTime <= TimeSpan.Zero)
			issues.Add($"TotalTime is invalid: {job.TotalTime}");

		if (job.RemainingTime > job.TotalTime)
			issues.Add($"RemainingTime ({job.RemainingTime}) exceeds TotalTime ({job.TotalTime})");

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"Active job has {issues.Count} issue(s)",
				string.Join("; ", issues)));

		return Task.FromResult(TestResult.Pass(
			$"'{job.Name}' at {job.PercentageComplete}%, {job.RemainingTime:hh\\:mm\\:ss} remaining of {job.TotalTime:hh\\:mm\\:ss}"));
	}
}
