using Connect3Dp.Validation.Specs;
using Connect3Dp.Validation.Tests;
using Spectre.Console;

namespace Connect3Dp.Validation.Reporting;

public static class TestReport
{
	public static void Render(ModelSpec spec, List<(ValidationTest Test, TestResult Result)> results)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule("[bold]Validation Report[/]").LeftJustified());
		AnsiConsole.WriteLine();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Test")
			.AddColumn("Result")
			.AddColumn("Message");

		foreach (var (test, result) in results)
		{
			string resultMarkup = result.Outcome switch
			{
				TestOutcome.Pass => "[green]PASS[/]",
				TestOutcome.Fail => "[red]FAIL[/]",
				TestOutcome.Skip => "[yellow]SKIP[/]",
				_ => "[grey]????[/]"
			};

			string message = result.Detail != null
				? $"{Markup.Escape(result.Message)}\n[dim]{Markup.Escape(result.Detail)}[/]"
				: Markup.Escape(result.Message);

			table.AddRow(
				Markup.Escape(test.Name),
				resultMarkup,
				message);
		}

		AnsiConsole.Write(table);

		int passed = results.Count(r => r.Result.Outcome == TestOutcome.Pass);
		int failed = results.Count(r => r.Result.Outcome == TestOutcome.Fail);
		int skipped = results.Count(r => r.Result.Outcome == TestOutcome.Skip);

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[green]{passed} passed[/], [red]{failed} failed[/], [yellow]{skipped} skipped[/] — {results.Count} total");

		AnsiConsole.WriteLine();

		if (failed == 0)
			AnsiConsole.MarkupLine("[bold green]All tests passed![/]");
		else
			AnsiConsole.MarkupLine($"[bold red]{failed} test(s) failed.[/]");
	}
}
