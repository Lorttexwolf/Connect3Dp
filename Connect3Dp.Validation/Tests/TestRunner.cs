using Connect3Dp.Validation.Reporting;
using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
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
			AnsiConsole.Write(new Rule($"[bold]{tier.Key} Tests[/]").LeftJustified());
			AnsiConsole.WriteLine();

			if (tier.Key == RiskTier.Destructive)
			{
				if (!await PrepareForDestructiveTests(ct))
				{
					foreach (var test in tier)
						results.Add((test, TestResult.Skip("Skipped: printer not printing")));
					continue;
				}
			}
			else
			{
				string tierDesc = tier.Key == RiskTier.ReadOnly
					? "These tests only read state and do not modify the printer."
					: "These tests will toggle lights or air duct mode (non-destructive).";

				if (!AnsiConsole.Confirm($"{tierDesc} Proceed?"))
				{
					foreach (var test in tier)
						results.Add((test, TestResult.Skip("Skipped by user")));
					continue;
				}
			}

			AnsiConsole.WriteLine();

			foreach (var test in tier)
			{
				if (tier.Key == RiskTier.Destructive)
				{
					AnsiConsole.MarkupLine($"[bold]Next:[/] {test.Name} - {test.Description}");
					if (!AnsiConsole.Confirm("  Execute this step?"))
					{
						results.Add((test, TestResult.Skip("Skipped by user")));
						continue;
					}
				}

				var result = await ExecuteTest(test, ct);
				results.Add((test, result));
				PrintResult(test, result);
			}

			AnsiConsole.WriteLine();
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

	private async Task<bool> PrepareForDestructiveTests(CancellationToken ct)
	{
		AnsiConsole.MarkupLine("[bold yellow]DESTRUCTIVE TESTS[/]");
		AnsiConsole.MarkupLine("These tests will pause, resume, and stop a print job.");
		AnsiConsole.MarkupLine("Please start a test print from your printer's touchscreen or slicer software.");
		AnsiConsole.WriteLine();

		if (!AnsiConsole.Confirm("Have you started a test print and want to proceed?"))
			return false;

		// Wait for printer to report Printing status
		bool isPrinting = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.SpinnerStyle(Style.Parse("yellow"))
			.StartAsync("Waiting for printer to report 'Printing' status...", async ctx =>
			{
				var timeout = TimeSpan.FromSeconds(120);
				var sw = System.Diagnostics.Stopwatch.StartNew();

				while (sw.Elapsed < timeout)
				{
					if (_connection.State.Status == MachineStatus.Printing)
						return true;

					await Task.Delay(1000, ct);
				}

				return false;
			});

		if (!isPrinting)
		{
			AnsiConsole.MarkupLine("[red]Printer did not report 'Printing' status within 120 seconds.[/]");
			return false;
		}

		AnsiConsole.MarkupLine("[green]Printer is printing. Proceeding with destructive tests.[/]");
		AnsiConsole.WriteLine();
		return true;
	}
}
