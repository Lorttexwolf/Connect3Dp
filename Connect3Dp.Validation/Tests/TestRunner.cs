using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Extensions;
using Lib3Dp.State;
using Spectre.Console;

namespace Connect3Dp.Validation.Tests;

public class TestRunner
{
	private readonly MachineConnection _connection;
	private readonly ModelSpec _spec;
	private readonly List<ValidationTest> _tests;

	public TestRunner(MachineConnection connection, ModelSpec spec, List<ValidationTest> tests)
	{
		_connection = connection;
		_spec = spec;
		_tests = tests;
	}

	public async Task<List<(ValidationTest Test, TestResult Result)>> RunAllAsync(CancellationToken ct)
	{
		var results = new List<(ValidationTest, TestResult)>();

		var tiers = _tests.GroupBy(t => t.Tier).OrderBy(g => g.Key);

		foreach (var tier in tiers)
		{
			if (tier.Key == RiskTier.Destructive)
			{
				AnsiConsole.WriteLine();
				if (!AnsiConsole.Confirm("Run destructive tests? (will start, pause, resume, and stop a print)"))
				{
					foreach (var test in tier)
						results.Add((test, TestResult.Skip("Skipped by user")));
					continue;
				}
				AnsiConsole.WriteLine();

				if (_connection.State.Status != MachineStatus.Idle)
				{
					AnsiConsole.MarkupLine($"[yellow]Printer is not idle (status: {_connection.State.Status}). Please clear the bed before proceeding.[/]");
					if (!AnsiConsole.Confirm("Is the bed clear and ready to print?", defaultValue: false))
					{
						foreach (var test in tier)
							results.Add((test, TestResult.Skip("Skipped: bed not confirmed clear")));
						continue;
					}
					AnsiConsole.WriteLine();
				}

				// Display material mapping before firing the print
				if (!string.IsNullOrEmpty(_spec.ValidationPrintFileName))
				{
					var job = _connection.State.LocalJobs.FirstOrDefault(j =>
						j.Name.Contains(_spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

					if (job.Name != null && job.MaterialsToPrint?.Count > 0)
					{
						var matches = _connection.State.FindMatchingSpools(job.MaterialsToPrint);

						var mapTable = new Table()
							.Border(TableBorder.Rounded)
							.AddColumn("Filament")
							.AddColumn("Material")
							.AddColumn("Color")
							.AddColumn("→ Slot");

						foreach (var (filamentId, match) in matches.Match)
						{
							var mat = job.MaterialsToPrint[filamentId].Material;
							mapTable.AddRow(
								filamentId.ToString(),
								Markup.Escape(mat.Name),
								Markup.Escape(mat.Color.Name ?? $"#{mat.Color.Hex}"),
								Markup.Escape($"{match.Location.MUID} / {match.Location.Slot}"));
						}

						foreach (var missing in matches.Missing)
						{
							var mat = job.MaterialsToPrint[missing].Material;
							mapTable.AddRow(
								missing.ToString(),
								Markup.Escape(mat.Name),
								Markup.Escape(mat.Color.Name ?? $"#{mat.Color.Hex}"),
								"[red]No match[/]");
						}

						AnsiConsole.Write(new Panel(mapTable)
							.Header("[bold]Material Mapping[/]")
							.Border(BoxBorder.Rounded));

						if (!AnsiConsole.Confirm("Is this material binding correct?", defaultValue: true))
						{
							foreach (var test in tier)
								results.Add((test, TestResult.Skip("Skipped: material binding not confirmed")));
							continue;
						}

						AnsiConsole.WriteLine();
					}
				}

				// Run as a pipeline — if any step fails, skip the rest
				bool pipelineBroken = false;
				foreach (var test in tier)
				{
					if (pipelineBroken)
					{
						var skip = TestResult.Skip("Skipped: previous step failed");
						results.Add((test, skip));
						PrintResult(test, skip);
						continue;
					}

					var result = await ExecuteTest(test, ct);
					results.Add((test, result));
					PrintResult(test, result);

					if (result.Outcome == TestOutcome.Fail)
						pipelineBroken = true;
				}

				continue;
			}

			foreach (var test in tier)
			{
				var result = await ExecuteTest(test, ct);
				results.Add((test, result));
				PrintResult(test, result);
			}
		}

		return results;
	}

	private async Task<TestResult> ExecuteTest(ValidationTest test, CancellationToken ct)
	{
		try
		{
			return await AnsiConsole.Status()
				.Spinner(Spinner.Known.Dots)
				.SpinnerStyle(Style.Parse("cyan"))
				.StartAsync($"Running {test.Name}...", async ctx =>
				{
					return await test.RunAsync(_connection, _spec, ct);
				});
		}
		catch (NotImplementedException)
		{
			return TestResult.Skip("Not yet implemented in connector");
		}
		catch (Exception ex)
		{
			return TestResult.Fail("Exception", ex.Message);
		}
	}

	private static void PrintResult(ValidationTest test, TestResult result)
	{
		string icon = result.Outcome switch
		{
			TestOutcome.Pass => "[green]PASS[/]",
			TestOutcome.Fail => "[red]FAIL[/]",
			TestOutcome.Skip => "[yellow]SKIP[/]",
			_ => "[grey]????[/]"
		};

		AnsiConsole.MarkupLine($"  {icon} {Markup.Escape(test.Name)}: {Markup.Escape(result.Message)}");

		if (result.Detail != null)
			AnsiConsole.MarkupLine($"       [dim]{Markup.Escape(result.Detail)}[/]");
	}
}
