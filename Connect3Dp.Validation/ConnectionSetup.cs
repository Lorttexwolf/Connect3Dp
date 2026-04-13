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

		var spec = ModelSpecs.All[modelName];

		// Nozzle confirmation
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold]Nozzle Configuration[/]");

		var nozzleDiameter = AnsiConsole.Prompt(
			new TextPrompt<double>("Installed nozzle diameter (mm)?")
				.DefaultValue(spec.DefaultNozzleDiameter)
				.Validate(d => d > 0 && d < 5, "Diameter must be between 0 and 5 mm"));

		var nozzleCount = AnsiConsole.Prompt(
			new TextPrompt<int>("Number of nozzles?")
				.DefaultValue(spec.ExpectedNozzleCount)
				.Validate(n => n >= 1 && n <= 4, "Nozzle count must be between 1 and 4"));

		spec = spec with { DefaultNozzleDiameter = nozzleDiameter, ExpectedNozzleCount = nozzleCount };

		// File store (temp directory for validation)
		var tempPath = Path.Combine(Path.GetTempPath(), "Connect3Dp.Validation", Guid.NewGuid().ToString("N")[..8]);
		var fileStore = new FileSystemMachineFileStore(new FileSystemMachineFileStoreOptions(tempPath, false));

		// Connection details — brand-specific
		MachineConnection connection = brand switch
		{
			PrinterBrand.BambuLab => SetupBambuLab(fileStore),
			PrinterBrand.ELEGOO => SetupELEGOO(fileStore, modelName),
			PrinterBrand.Creality => SetupCreality(fileStore),
			_ => throw new InvalidOperationException($"Unknown brand: {brand}")
		};

		return (connection, spec);
	}

	private static MachineConnection SetupBambuLab(FileSystemMachineFileStore fileStore)
	{
		var ip = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]IP address[/]:")
				.Validate(v => !string.IsNullOrWhiteSpace(v), "IP address is required"));

		var serial = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]serial number[/]:")
				.Validate(v => v.Length >= 3, "Serial number must be at least 3 characters"));

		var accessCode = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]access code[/]:")
				.Secret()
				.Validate(v => !string.IsNullOrWhiteSpace(v), "Access code is required"));

		var config = new BBLMachineConfiguration(null, ip, serial, accessCode);
		return BBLMachineConnection.LAN(fileStore, config);
	}

	private static MachineConnection SetupELEGOO(FileSystemMachineFileStore fileStore, string modelName)
	{
		var ip = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]IP address[/]:")
				.Validate(v => !string.IsNullOrWhiteSpace(v), "IP address is required"));

		var serial = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]serial number[/]:"));

		var kind = modelName switch
		{
			"Centauri Carbon" => ELEGOOMachineKind.CentauriCarbon,
			_ => ELEGOOMachineKind.CentauriCarbon
		};

		var config = new ELEGOOMachineConfiguration(null, kind, serial, ip);
		return new ELEGOOMachineConnector(fileStore, config);
	}

	private static MachineConnection SetupCreality(FileSystemMachineFileStore fileStore)
	{
		var ip = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]IP address[/]:")
				.Validate(v => !string.IsNullOrWhiteSpace(v), "IP address is required"));

		var serial = AnsiConsole.Prompt(
			new TextPrompt<string>("Printer [bold]serial number[/]:"));

		var config = new CrealityK1CConfiguration(null, ip, serial);
		return CrealityK1CConnection.CreateFromConfiguration(fileStore, config);
	}
}
