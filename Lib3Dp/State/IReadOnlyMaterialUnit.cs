using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public interface IReadOnlyMaterialUnit : IUniquelyIdentifiable
	{
		string? Model { get; }
		int Capacity { get; }
		[JsonConverter(typeof(JsonStringEnumConverter))] MaterialUnitCapabilities Capabilities { get; }
		IEnumerable<IReadOnlySpool> Trays { get; }
		double? HumidityPercent { get; }
		double? TemperatureC { get; }
		HeatingConstraints? HeatingConstraints { get; }
		HeatingJob? HeatingJob { get; }
		IEnumerable<HeatingSchedule> HeatingSchedule { get; }
	}
}
