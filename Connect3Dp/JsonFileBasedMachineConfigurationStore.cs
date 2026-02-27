using Connect3Dp.Services;
using Lib3Dp.Configuration;
using Lib3Dp.Connectors;
using Lib3Dp.Files;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

public class JsonFileBasedMachineConfigurationStore : IMachineConfigurationStore
{
	private static readonly JsonSerializerOptions JsonOptions = new() 
	{
		PropertyNameCaseInsensitive = true
	};
	private static readonly JsonSerializerOptions JsonWriteOptions = new() 
	{ 
		WriteIndented = true
	};

	private readonly ILogger<JsonFileBasedMachineConfigurationStore> Logger;
	private readonly string FilePath;
	private readonly object Lock = new();

	// In-memory map of stored configurations
	private readonly Dictionary<string, ConfigurationWithDiscrimination> _store = new();

	public JsonFileBasedMachineConfigurationStore(ILogger<JsonFileBasedMachineConfigurationStore> logger, string filePath)
	{
		Logger = logger;
		FilePath = filePath;
	}

	public async Task StoreConfiguration(string machineId, ConfigurationWithDiscrimination configuration)
	{
		if (string.IsNullOrWhiteSpace(machineId)) throw new ArgumentException("machineId must be provided", nameof(machineId));
		if (configuration.Configuration == null) throw new ArgumentNullException(nameof(configuration));

		lock (Lock)
		{
			_store[machineId] = configuration;
		}

		await PersistToFile().ConfigureAwait(false);
	}

	public async Task RemoveConfiguration(string machineId)
	{
		if (string.IsNullOrWhiteSpace(machineId)) return;

		bool wasRemoved;
		lock (Lock)
		{
			wasRemoved = _store.Remove(machineId);
		}

		if (wasRemoved)
		{
			await PersistToFile().ConfigureAwait(false);
		}
	}

	private async Task PersistToFile()
	{
		JsonArray printerEntries = new();

		lock (Lock)
		{
			foreach (var (machineId, configuration) in _store)
			{
				printerEntries.Add(new JsonObject
				{
					["id"] = machineId,
					["discrimination"] = configuration.Discrimination,
					["configuration"] = JsonSerializer.SerializeToNode(configuration.Configuration, configuration.Configuration.GetType(), JsonOptions)
				});
			}
		}

		string fileContent = new JsonObject { ["printers"] = printerEntries }.ToJsonString(JsonWriteOptions);

		try
		{
			var directory = Path.GetDirectoryName(FilePath);
			if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

			await File.WriteAllTextAsync(FilePath, fileContent).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Logger?.LogError(ex, "Failed to persist machine configurations to {FilePath}", FilePath);
		}
	}

	public async Task<MachineIDWithConfigurationWithDiscrimination[]> LoadConfigurations()
	{
		var configurations = new List<MachineIDWithConfigurationWithDiscrimination>();

		if (!File.Exists(FilePath)) return [];

		string fileContent = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);

		try
		{
			if ((JsonNode.Parse(fileContent) as JsonObject)?["printers"] is not JsonArray printerEntries)
			{
				return [];
			}

			foreach (var printerEntry in printerEntries.OfType<JsonObject>())
			{
				string? machineId = printerEntry["id"]?.GetValue<string>();
				string? discrimination = printerEntry["discrimination"]?.GetValue<string>();
				JsonNode? configNode = printerEntry["configuration"];

				if (string.IsNullOrWhiteSpace(machineId) || string.IsNullOrWhiteSpace(discrimination) || configNode == null) continue;
				if (!ConfigurableConnections.TryGetConfigurationType(discrimination, out var configType) || configType == null) continue;

				var configuration = configNode.Deserialize(configType, JsonOptions)!;

				configurations.Add(new MachineIDWithConfigurationWithDiscrimination(machineId, new ConfigurationWithDiscrimination(configuration, discrimination)));
			}
		}
		catch (Exception ex)
		{
			Logger?.LogError(ex, "Failed to load machine configurations from {FilePath}", FilePath);
		}

		// Update in-memory store with loaded configs for future mutations
		lock (Lock)
		{
			_store.Clear();
			foreach (var item in configurations)
			{
				_store[item.MachineID] = item.ConfigurationWithDiscrimination;
			}
		}

		return configurations.ToArray();
	}
}
