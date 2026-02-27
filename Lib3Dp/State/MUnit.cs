using PartialBuilderSourceGen.Attributes;
using PartialBuilderSourceGen.Attributes;
using System.Text.Json.Serialization;
using Lib3Dp.Utilities;

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
			return obj is MUnit m && Equals(m);
		}

		public bool Equals(MUnit? other)
		{
			if (other is null) return false;

			if (!EqualityComparer<string>.Default.Equals(ID, other.ID))
				return false;

			if (Capacity != other.Capacity)
				return false;

			if (!EqualityComparer<string?>.Default.Equals(Model, other.Model))
				return false;

			if (Capabilities != other.Capabilities)
				return false;

			if (!EqualityComparer<HeatingConstraints?>.Default.Equals(HeatingConstraints, other.HeatingConstraints))
				return false;

			// Compare Trays by contents (same keys and values)
			if (!CollectionUtils.AreDictionariesEqual(Trays, other.Trays))
				return false;

			if (!EqualityComparer<double?>.Default.Equals(HumidityPercent, other.HumidityPercent))
				return false;

			if (!EqualityComparer<double?>.Default.Equals(TemperatureC, other.TemperatureC))
				return false;

			if (!EqualityComparer<HeatingJob?>.Default.Equals(HeatingJob, other.HeatingJob))
				return false;

			if (!HeatingSchedule.SetEquals(other.HeatingSchedule))
				return false;

			return true;
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
