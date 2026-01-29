using PartialBuilderSourceGen;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	public class MachineNozzle : IEquatable<MachineNozzle?>
	{
		public required int Number { get; set; }
		public required double Diameter { get; set; }
		public MaterialLocation? Material { get; set; }

		public override bool Equals(object? obj)
		{
			return Equals(obj as MachineNozzle);
		}

		public bool Equals(MachineNozzle? other)
		{
			return other is not null &&
				   Number == other.Number &&
				   Diameter == other.Diameter;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Number, Diameter);
		}

		public static bool operator ==(MachineNozzle? left, MachineNozzle? right)
		{
			return EqualityComparer<MachineNozzle>.Default.Equals(left, right);
		}

		public static bool operator !=(MachineNozzle? left, MachineNozzle? right)
		{
			return !(left == right);
		}
	}
}
