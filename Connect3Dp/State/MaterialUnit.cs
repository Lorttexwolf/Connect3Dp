using Connect3Dp.SourceGeneration.UpdateGen;
using PartialSourceGen;
using System.Text.Json.Serialization;

namespace Connect3Dp.State
{
    public class MaterialUnitConfiguration
    {
        public required string ID;
        public required int Slots;
        // TODO: Add filaments 
        public required HashSet<HeatingSchedule> HeatingSchedule;
    }

    [GenerateUpdate]
    public partial class MaterialUnit : IReadOnlyMaterialUnit
    {
        public required string ID { get; set; }
        public required int Capacity { get; set; }

        public string? Model { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MaterialUnitFeatures Features { get; internal set; }

        public HeatingConstraints? HeatingConstraints { get; internal set; }

        public Dictionary<int, Material> Loaded { get; } = [];

        public double? HumidityPercent { get; set; }
        public double? TemperatureC { get; set; }
        
        public HeatingJob? HeatingJob { get; set; }

        public HashSet<HeatingSchedule> HeatingSchedule { get; init;  } = [];

        IReadOnlyDictionary<int, Material> IReadOnlyMaterialUnit.Loaded => Loaded;
        IEnumerable<HeatingSchedule> IReadOnlyMaterialUnit.HeatingSchedule => HeatingSchedule;

        public object GetConfiguration()
        {
            return new MaterialUnitConfiguration()
            {
                ID = ID,
                HeatingSchedule = [],
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

            if (!EqualityComparer<HeatingJob?>.Default.Equals(HeatingJob, other.HeatingJob))
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
