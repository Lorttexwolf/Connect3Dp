using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.ReadOnly;

public class PrintHistoryTest : ValidationTest
{
	public override string Name => "Print History";
	public override string Description => "Verify print history is populated when the capability is present";
	public override RiskTier Tier => RiskTier.ReadOnly;

	public override Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		bool shouldHaveHistory = spec.ExpectedCapabilities.HasFlag(MachineCapabilities.PrintHistory);
		bool hasHistory = connection.State.Capabilities.HasFlag(MachineCapabilities.PrintHistory);

		if (!shouldHaveHistory)
		{
			return Task.FromResult(hasHistory
				? TestResult.Fail("PrintHistory capability present but should be absent per spec")
				: TestResult.Pass("PrintHistory correctly absent for this model"));
		}

		if (!hasHistory)
			return Task.FromResult(TestResult.Fail("PrintHistory capability expected but not present"));

		var history = connection.State.JobHistory.ToList();

		if (history.Count == 0)
			return Task.FromResult(TestResult.Pass("PrintHistory capability present but history is empty (may be a fresh machine)"));

		var issues = new List<string>();

		foreach (var entry in history)
		{
			if (string.IsNullOrWhiteSpace(entry.Name))
				issues.Add("Entry with empty name found");

			if (entry.EndedAt > DateTime.Now.AddMinutes(5))
				issues.Add($"Entry '{entry.Name}' has future EndedAt: {entry.EndedAt}");
		}

		if (issues.Count > 0)
			return Task.FromResult(TestResult.Fail(
				$"{issues.Count} history issue(s)",
				string.Join("; ", issues)));

		var recent = history.Take(3).Select(h =>
			$"'{h.Name}' ({(h.IsSuccess ? "OK" : "FAIL")}, {h.EndedAt:g})");

		return Task.FromResult(TestResult.Pass(
			$"{history.Count} entries. Recent: {string.Join("; ", recent)}"));
	}
}
