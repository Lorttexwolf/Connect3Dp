using Connect3Dp.Validation.Specs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Connect3Dp.Validation;

public class SavedConfiguration
{
	public PrinterBrand Brand { get; set; }
	public string ModelName { get; set; } = "";
	public string IP { get; set; } = "";
	public string Serial { get; set; } = "";
	public string AccessCode { get; set; } = "";

	private static readonly string FilePath = Path.Combine(
		AppContext.BaseDirectory, "last_config.json");

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public void Save()
	{
		try
		{
			File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
		}
		catch { }
	}

	public static SavedConfiguration? Load()
	{
		try
		{
			if (!File.Exists(FilePath)) return null;
			return JsonSerializer.Deserialize<SavedConfiguration>(File.ReadAllText(FilePath), JsonOptions);
		}
		catch { return null; }
	}
}
