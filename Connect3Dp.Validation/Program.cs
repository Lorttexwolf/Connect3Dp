using Connect3Dp.Validation;
using Connect3Dp.Validation.Reporting;
using Connect3Dp.Validation.Specs;
using Connect3Dp.Validation.Tests;
using Connect3Dp.Validation.Tests.ReadOnly;
using Connect3Dp.Validation.Tests.NonDestructive;
using Connect3Dp.Validation.Tests.Destructive;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using Spectre.Console;

// Suppress all library logging — test results capture everything relevant,
// and log output interleaves with Spectre.Console spinners.
Logger.SetGlobalMinimumLevel(Logger.Level.None);

// Connection setup
var (connection, spec) = ConnectionSetup.Run();
AnsiConsole.WriteLine();

// Show spec
var specTable = new Table()
	.Border(TableBorder.Rounded)
	.AddColumn("Property")
	.AddColumn("Value");

specTable.AddRow("Brand", spec.Brand.ToString());
specTable.AddRow("Model", spec.ModelName);
specTable.AddRow("Expected Capabilities", spec.ExpectedCapabilities.ToString());
specTable.AddRow("Absent Capabilities", spec.ExplicitlyAbsentCapabilities.ToString());
specTable.AddRow("SD/USB Required", spec.RequiresSDOrUSB.ToString());

if (spec.ExpectedHeatingConstraints != null)
{
	foreach (var (name, constraints) in spec.ExpectedHeatingConstraints)
		specTable.AddRow($"Temp: {name}", constraints.ToString());
}

AnsiConsole.Write(new Panel(specTable)
	.Header($"[bold]{spec.ModelName} Spec[/]")
	.Border(BoxBorder.Rounded));
AnsiConsole.WriteLine();

// Warning for skeleton connectors
if (spec.Brand is PrinterBrand.ELEGOO or PrinterBrand.Creality)
{
	AnsiConsole.MarkupLine("[yellow]Note: This connector is under active development. Many tests will be skipped.[/]");
	AnsiConsole.WriteLine();
}

// Connect and wait for initial data
var connectResult = await connection.Connect();
if (!connectResult.Success)
{
	AnsiConsole.MarkupLine($"[red]Failed to connect: {connectResult.Reasoning}[/]");
	return;
}

// Wait for material unit data to arrive
await AnsiConsole.Status()
	.Spinner(Spinner.Known.Dots)
	.SpinnerStyle(Style.Parse("cyan"))
	.StartAsync("Waiting for printer state...", async ctx =>
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (connection.State.Status == MachineStatus.Connecting && sw.Elapsed < TimeSpan.FromSeconds(15))
			await Task.Delay(500);
	});

// Dynamic MU (AMS) detection
var detectedUnits = connection.State.MaterialUnits.ToList();

if (detectedUnits.Count > 0)
{
	var muTable = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn("ID")
		.AddColumn("Model")
		.AddColumn("Capacity")
		.AddColumn("Capabilities")
		.AddColumn("Loaded Materials");

	foreach (var unit in detectedUnits)
	{
		var loadedMaterials = unit.Trays.Count > 0
			? string.Join("\n", unit.Trays.OrderBy(t => t.Key).Select(t =>
				$"Slot {t.Key}: {t.Value.Material.Name} {t.Value.Material.Color.Name ?? $"#{t.Value.Material.Color.Hex}"}"))
			: "—";

		muTable.AddRow(
			Markup.Escape(unit.ID),
			Markup.Escape(unit.Model ?? "Unknown"),
			unit.Capacity.ToString(),
			unit.Capabilities.ToString(),
			Markup.Escape(loadedMaterials));
	}

	AnsiConsole.Write(new Panel(muTable)
		.Header("[bold]Detected Material Units[/]")
		.Border(BoxBorder.Rounded));

	if (!AnsiConsole.Confirm("Are these material units correct?", defaultValue: true))
	{
		AnsiConsole.MarkupLine("[yellow]Continuing anyway — AMS tests may produce unexpected results.[/]");
	}

	AnsiConsole.WriteLine();
}
else
{
	AnsiConsole.MarkupLine("[dim]No material units (AMS) detected — AMS tests will be skipped.[/]");
	AnsiConsole.WriteLine();
}

// Build test suite
var tests = new List<ValidationTest>
{
	// Read-only
	new ConnectionTest(),
	new StatusTest(),
	new CapabilityPresenceTest(),
	new CapabilityAbsenceTest(),
	new HeatingElementsTest(),
	new TemperatureConstraintsTest(),

	new AMSDetectionTest(),
	new AMSCapabilityValidationTest(),
	new AMSHeatingToggleTest(),

	new LocalJobsTest(),
	new LocalJobPrintReadinessTest(),
	new NozzleInfoTest(),
	new ExtruderInfoTest(),
	new ActiveJobInfoTest(),
	new PrintHistoryTest(),
	new AMSHeatingConstraintsTest(),

	// Non-destructive
	new LightToggleTest(),
	new AirDuctToggleTest(),

	new USBStorageRemovalTest(),

	// Destructive
	new PrintLocalFileTest(),
};

// Test selection
var prompt = new MultiSelectionPrompt<ValidationTest>()
	.Title("Select tests to run [dim](Space = toggle, Enter = confirm)[/]")
	.NotRequired()
	.PageSize(30)
	.MoreChoicesText("[dim]Scroll for more[/]")
	.InstructionsText("[dim](Space) toggle  [green](Enter)[/] confirm[/]")
	.UseConverter(t => t.Name);

prompt.AddChoices(tests);

foreach (var t in tests)
	prompt.Select(t);

tests = AnsiConsole.Prompt(prompt);

if (tests.Count == 0)
{
	AnsiConsole.MarkupLine("[yellow]No tests selected. Exiting.[/]");
	try { await connection.Disconnect(); } catch { }
	return;
}

AnsiConsole.WriteLine();

// Run
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var runner = new TestRunner(connection, spec, tests);
var results = await runner.RunAllAsync(cts.Token);

// Report
TestReport.Render(spec, results);

// Cleanup
try { await connection.Disconnect(); } catch { }
