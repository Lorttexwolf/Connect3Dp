using PartialSourceGen;
using System.Text.Json.Serialization;

namespace Connect3Dp.State
{
    public class MaterialUnitConfiguration
    {
        public required string ID;
        public required int Slots;
        // Add filaments 
        public required HashSet<IHeatingSchedule> HeatingSchedule;
    }

    public partial class MaterialUnit(string ID, int Capacity) : IReadOnlyMaterialUnit
    {
        // TODO: Max and min heating tempC. HeatingConstraints

        public string ID { get; set; } = ID;

        public string? Model { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MaterialUnitFeatures Features { get; internal set; }

        public HeatingConstraints? HeatingConstraints { get; internal set; }

        public int Capacity { get; set; } = Capacity;

        public Dictionary<int, Material> Loaded { get; } = [];

        public double? HumidityPercent { get; set; }
        public double? TemperatureC { get; set; }
        
        public PartialHeatingSettings? HeatingJob { get; set; }

        public HashSet<HeatingSchedule> HeatingSchedule { get; init;  } = [];

        IReadOnlyDictionary<int, Material> IReadOnlyMaterialUnit.Loaded => Loaded;

        IEnumerable<IHeatingSchedule> IReadOnlyMaterialUnit.HeatingSchedule => HeatingSchedule;

        public object GetConfiguration()
        {
            return new MaterialUnitConfiguration()
            {
                ID = ID,
                HeatingSchedule = new HashSet<IHeatingSchedule>(),
                Slots = Capacity
            };
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MaterialUnit);
        }

        public bool Equals(MaterialUnit? other)
        {
            if (other is null)
                return false;

            if (!ID.Equals(other.ID))
                return false;

            if (Features != other.Features)
                return false;

            if (Capacity != other.Capacity)
                return false;

            if (!Loaded.SequenceEqual(other.Loaded))
                return false;

            if (HumidityPercent != other.HumidityPercent)
                return false;

            if (TemperatureC != other.TemperatureC)
                return false;

            if (!EqualityComparer<PartialHeatingSettings?>.Default.Equals(HeatingJob, other.HeatingJob))
                return false;

            if (!HeatingSchedule.SetEquals(other.HeatingSchedule))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID, Features, Capacity, Loaded, HumidityPercent, TemperatureC, HeatingJob, HeatingSchedule);
        }
    }
}
