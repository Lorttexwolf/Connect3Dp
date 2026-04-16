using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Connectors.BambuLab;
using Lib3Dp.Connectors.Creality;
using Lib3Dp.Connectors.ELEGOO;
using Lib3Dp.Files;
using Spectre.Console;

namespace Connect3Dp.Validation;

public static class ConnectionSetup
{
	public static (MachineConnection Connection, ModelSpec Spec) Run()
	{
		// Check for saved configuration
		var saved = SavedConfiguration.Load();
		if (saved != null && ModelSpecs.All.ContainsKey(saved.ModelName))
		{
			AnsiConsole.MarkupLine($"[bold]Last used:[/] {saved.Brand} {saved.ModelName} @ {saved.IP}");
			if (AnsiConsole.Confirm("Use this configuration?", defaultValue: true))
			{
				var spec = ModelSpecs.All[saved.ModelName];
				var connection = CreateConnection(saved.Brand, saved.ModelName, saved.IP, saved.Serial, saved.AccessCode);
				return (connection, spec);
			}
			AnsiConsole.WriteLine();
		}

		// Brand selection
		var brand = AnsiConsole.Prompt(
			new SelectionPrompt<PrinterBrand>()
				.Title("Select [bold]printer brand[/]:")
				.AddChoices(PrinterBrand.BambuLab, PrinterBrand.ELEGOO, PrinterBrand.Creality));

		// Model selection
		var models = ModelSpecs.GetModelsForBrand(brand).ToList();
		var modelName = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("Select [bold]printer model[/]:")
				.AddChoices(models));

		var selectedSpec = ModelSpecs.All[modelName];

		// Nozzle confirmation
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold]Nozzle Configuration[/]");

		var nozzleDiameter = AnsiConsole.Prompt(
			new TextPrompt<double>("Installed nozzle diameter (mm)?")
				.DefaultValue(selectedSpec.DefaultNozzleDiameter)
				.Validate(d => d > 0 && d < 5, "Diameter must be between 0 and 5 mm"));

		var nozzleCount = AnsiConsole.Prompt(
			new TextPrompt<int>("Number of nozzles?")
				.DefaultValue(selectedSpec.ExpectedNozzleCount)
				.Validate(n => n >= 1 && n <= 4, "Nozzle count must be between 1 and 4"));

		selectedSpec = selectedSpec with { DefaultNozzleDiameter = nozzleDiameter, ExpectedNozzleCount = nozzleCount };

		// Connection details
		var ip = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]IP address[/]:")
				.Validate(v => !string.IsNullOrWhiteSpace(v), "IP address is required"));

		var serial = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]serial number[/]:")
				.Validate(v => v.Length >= 3, "Serial number must be at least 3 characters"));

		var accessCode = brand == PrinterBrand.BambuLab
			? AnsiConsole.Prompt(
				new TextPrompt<string>("Printer [bold]access code[/]:")
					.Secret()
					.Validate(v => !string.IsNullOrWhiteSpace(v), "Access code is required"))
			: "";

		// Save for next time
		new SavedConfiguration
		{
			Brand = brand,
			ModelName = modelName,
			IP = ip,
			Serial = serial,
			AccessCode = accessCode
		}.Save();

		var conn = CreateConnection(brand, modelName, ip, serial, accessCode);
		return (conn, selectedSpec);
	}

	private static MachineConnection CreateConnection(PrinterBrand brand, string modelName, string ip, string serial, string accessCode)
	{
		var tempPath = Path.Combine(Path.GetTempPath(), "Connect3Dp.Validation", Guid.NewGuid().ToString("N")[..8]);
		var fileStore = new FileSystemMachineFileStore(new FileSystemMachineFileStoreOptions(tempPath, false));

		return brand switch
		{
			PrinterBrand.BambuLab => BBLMachineConnection.LAN(fileStore, new BBLMachineConfiguration(null, ip, serial, accessCode)),
			PrinterBrand.ELEGOO => new ELEGOOMachineConnector(fileStore, new ELEGOOMachineConfiguration(null, modelName switch
			{
				"Centauri Carbon" => ELEGOOMachineKind.CentauriCarbon,
				_ => ELEGOOMachineKind.CentauriCarbon
			}, serial, ip)),
			PrinterBrand.Creality => CrealityK1CConnection.CreateFromConfiguration(fileStore, new CrealityK1CConfiguration(null, ip, serial)),
			_ => throw new InvalidOperationException($"Unknown brand: {brand}")
		};
	}
}
