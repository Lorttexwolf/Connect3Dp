using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public interface IMaterialUnit : IUniquelyIdentifiable
	{
		string? Model { get; }
		int Capacity { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MUCapabilities Capabilities { get; }
		IReadOnlyDictionary<int, Spool> Trays { get; }
		double? HumidityPercent { get; }
		double? TemperatureC { get; }
		HeatingConstraints? HeatingConstraints { get; }
		HeatingJob? HeatingJob { get; }
		IReadOnlySet<HeatingSchedule> HeatingSchedule { get; }
	}
}
