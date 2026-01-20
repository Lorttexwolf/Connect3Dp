using PartialSourceGen;
using System.Text.Json.Serialization;

namespace Connect3Dp
{
    public class MaterialUnitConfiguration
    {
        public required string UID;
        public required int Slots;
        public required HashSet<HeatingSchedule> HeatingSchedule;
    }

    [Partial(IncludeRequiredProperties = true)]
    public class MaterialUnit(string UID, int Capacity) : IConfigurable, IEquatable<MaterialUnit?>
    {
        public string UID { get; set; } = UID;

        public string? Model { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MaterialUnitFeatures Features { get; internal set; }

        public int Capacity { get; set; } = Capacity;

        [PartialReference<Material, PartialMaterial>()]
        public List<Material> Loaded { get; } = [];

        public double? HumidityPercent { get; set; }
        public double? TemperatureC { get; set; }
        
        public PartialHeatingSettings? ActiveHeatingSettings { get; set; }

        public HashSet<HeatingSchedule> HeatingSchedule { get; init;  } = [];

        public object GetConfiguration()
        {
            return new MaterialUnitConfiguration()
            {
                UID = UID,
                HeatingSchedule = HeatingSchedule,
                Slots = Capacity
            };
        }

        public static object MakeWithConfiguration(object configuration)
        {
            if (configuration is MaterialUnitConfiguration mConfiguration)
            {
                var mU = new MaterialUnit(mConfiguration.UID, mConfiguration.Slots)
                {
                    UID = mConfiguration.UID,
                    HeatingSchedule = mConfiguration.HeatingSchedule
                };

                return mU;
            }
            else
            {
                throw new ArgumentException("Must be of type MaterialUnitConfiguration", nameof(configuration));
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MaterialUnit);
        }

        public bool Equals(MaterialUnit? other)
        {
            if (other is null)
                return false;

            if (!UID.Equals(other.UID))
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

            if (!EqualityComparer<PartialHeatingSettings?>.Default.Equals(ActiveHeatingSettings, other.ActiveHeatingSettings))
                return false;

            if (!HeatingSchedule.SetEquals(other.HeatingSchedule))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UID, Features, Capacity, Loaded, HumidityPercent, TemperatureC, ActiveHeatingSettings, HeatingSchedule);
        }

        public static bool operator ==(MaterialUnit? left, MaterialUnit? right)
        {
            return EqualityComparer<MaterialUnit>.Default.Equals(left, right);
        }

        public static bool operator !=(MaterialUnit? left, MaterialUnit? right)
        {
            return !(left == right);
        }
    }

    [Flags]
    public enum MaterialUnitFeatures
    {
        AutomaticFeeding = 1 << 0,
        Heating = 1 << 1,
        Heating_TargetTemp = 1 << 6, // Bambu Lab doesn't support this as of 1/16/2026 :skull:
        Heating_CanSpin = 1 << 2,
        Heating_CanInUse = 1 << 3,
        Humidity = 1 << 4,
        Temperature = 1 << 5
    }
}
