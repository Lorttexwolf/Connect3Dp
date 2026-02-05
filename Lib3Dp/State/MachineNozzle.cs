using PartialBuilderSourceGen;

namespace Lib3Dp.State
{

	[GeneratePartialBuilder]
	internal class MachineNozzle : IMachineNozzle, IEquatable<MachineNozzle?>
	{
		[PartialBuilderDictKey]
		public required int Number { get; set; }
		public required double Diameter { get; set; }

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

		public bool Equals(IMachineNozzle? other)
		{
			return Equals(other as MachineNozzle);
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
