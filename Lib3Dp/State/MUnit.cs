using PartialBuilderSourceGen.Attributes;
using PartialBuilderSourceGen.Attributes;
using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public class MaterialUnitConfiguration
	{
		public required string ID;
		public required int Slots;
		public required Dictionary<int, Spool> Trays;
		public required HashSet<HeatingSchedule> HeatingSchedule;
	}

	[GeneratePartialBuilder]
	public partial class MUnit : IMaterialUnit
	{
		[PartialBuilderDictKey]
		public required string ID { get; set; }
		public required int Capacity { get; set; }

		public string? Model { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public MUCapabilities Capabilities { get; internal set; }

		public HeatingConstraints? HeatingConstraints { get; internal set; }

		public Dictionary<int, Spool> Trays { get; set; } = [];

		public double? HumidityPercent { get; set; }
		public double? TemperatureC { get; set; }

		public HeatingJob? HeatingJob { get; set; }

		public HashSet<HeatingSchedule> HeatingSchedule { get; init; } = [];

		IReadOnlyDictionary<int, Spool> IMaterialUnit.Trays => Trays;
		IReadOnlySet<HeatingSchedule> IMaterialUnit.HeatingSchedule => HeatingSchedule;

		public object GetConfiguration()
		{
			return new MaterialUnitConfiguration()
			{
				ID = ID,
				Trays = this.Trays,
				HeatingSchedule = this.HeatingSchedule,
				Slots = Capacity
			};
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as MUnit);
		}

		public bool Equals(MUnit? other)
		{
			return other is not null &&
				   ID == other.ID &&
				   Capacity == other.Capacity &&
				   Model == other.Model &&
				   Capabilities == other.Capabilities &&
				   EqualityComparer<HeatingConstraints?>.Default.Equals(HeatingConstraints, other.HeatingConstraints) &&
				   EqualityComparer<Dictionary<int, Spool>>.Default.Equals(Trays, other.Trays) &&
				   HumidityPercent == other.HumidityPercent &&
				   TemperatureC == other.TemperatureC &&
				   EqualityComparer<HeatingJob?>.Default.Equals(HeatingJob, other.HeatingJob) &&
				   HeatingSchedule.SetEquals(other.HeatingSchedule);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ID, Capabilities, Capacity, Trays, HumidityPercent, TemperatureC, HeatingJob, HeatingSchedule);
		}

		public static bool operator ==(MUnit? left, MUnit? right)
		{
			return EqualityComparer<MUnit>.Default.Equals(left, right);
		}

		public static bool operator !=(MUnit? left, MUnit? right)
		{
			return !(left == right);
		}

	}
}
