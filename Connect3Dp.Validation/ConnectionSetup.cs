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
