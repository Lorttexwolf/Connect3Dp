using System.Text.Json.Serialization;

namespace Connect3Dp.State
{
    public interface IReadOnlyMaterialUnit : IUniquelyIdentifiable
    {
        string? Model { get; }
        [JsonConverter(typeof(JsonStringEnumConverter))] MaterialUnitFeatures Features { get; }
        int Capacity { get; }
        IReadOnlyDictionary<int, Material> Loaded { get; }
        double? HumidityPercent { get; }
        double? TemperatureC { get; }
        HeatingConstraints? HeatingConstraints { get; }
        PartialHeatingSettings? HeatingJob { get; }
        IEnumerable<IHeatingSchedule> HeatingSchedule { get; }
    }
}
