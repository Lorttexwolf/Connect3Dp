using Connect3Dp.Validation;
using Connect3Dp.Validation.Reporting;
using Connect3Dp.Validation.Specs;
using Connect3Dp.Validation.Tests;
using Connect3Dp.Validation.Tests.ReadOnly;
using Connect3Dp.Validation.Tests.NonDestructive;
using Connect3Dp.Validation.Tests.Destructive;
using Spectre.Console;

// Banner
AnsiConsole.Write(new FigletText("Connect3Dp").Color(Color.Cyan1));
AnsiConsole.Write(new FigletText("Validator").Color(Color.Grey));
AnsiConsole.WriteLine();

// Safety disclaimer
AnsiConsole.Write(new Panel(
	"[yellow]This tool communicates with a real 3D printer.[/]\n" +
	"Read-only and non-destructive tests are safe.\n" +
	"Destructive tests (pause/resume/stop) require you to manually start a print first.\n" +
	"[bold]This tool will NEVER auto-start a print.[/]")
	.Header("[bold yellow]Safety Notice[/]")
	.Border(BoxBorder.Rounded));
AnsiConsole.WriteLine();

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
specTable.AddRow("AMS", spec.ExpectedAMSModel ?? "None (Standalone)");

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
	new AMSHeatingToggleTest(),

	// Destructive
	new PrintLocalFileTest(),
	new PauseTest(),
	new ResumeTest(),
	new StopTest(),
	new MarkAsIdleTest(),
};

// Run
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var runner = new TestRunner(connection, spec, tests);
var results = await runner.RunAllAsync(cts.Token);

// Report
TestReport.Render(spec, results);

// Cleanup
try { await connection.Disconnect(); } catch { }
