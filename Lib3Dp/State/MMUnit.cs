using PartialBuilderSourceGen;
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
	public partial class MMUnit : IReadOnlyMaterialUnit
	{
		[PartialBuilderDictKey]
		public required string ID { get; set; }
		public required int Capacity { get; set; }

		public string? Model { get; set; }

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public MaterialUnitCapabilities Capabilities { get; internal set; }

		public HeatingConstraints? HeatingConstraints { get; internal set; }

		public Dictionary<int, Spool> Trays { get; } = [];

		public double? HumidityPercent { get; set; }
		public double? TemperatureC { get; set; }

		public HeatingJob? HeatingJob { get; set; }

		public HashSet<HeatingSchedule> HeatingSchedule { get; init; } = [];

		IEnumerable<HeatingSchedule> IReadOnlyMaterialUnit.HeatingSchedule => HeatingSchedule;
		IEnumerable<IReadOnlySpool> IReadOnlyMaterialUnit.Trays => Trays.Values;

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
			return Equals(obj as MMUnit);
		}

		public bool Equals(MMUnit? other)
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

		public static bool operator ==(MMUnit? left, MMUnit? right)
		{
			return EqualityComparer<MMUnit>.Default.Equals(left, right);
		}

		public static bool operator !=(MMUnit? left, MMUnit? right)
		{
			return !(left == right);
		}

	}
}
