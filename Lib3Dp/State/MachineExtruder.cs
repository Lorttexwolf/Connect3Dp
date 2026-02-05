using PartialBuilderSourceGen;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	internal class MachineExtruder : IMachineExtruder, IEquatable<MachineExtruder?>
	{
		[PartialBuilderDictKey]
		public required int Number { get; set; }
		public required HeatingConstraints HeatingConstraint { get; set; }

		public required double TempC { get; set; }
		public double? TargetTempC { get; set; }

		public int? NozzleNumber { get; set; }

		public SpoolLocation? LoadedSpool { get; set; }

		public override bool Equals(object? obj)
		{
			return Equals(obj as MachineExtruder);
		}

		public bool Equals(IMachineExtruder? other)
		{
			return this.Equals(other as MachineExtruder);
		}

		public bool Equals(MachineExtruder? other)
		{
			return other is not null &&
				   Number == other.Number &&
				   HeatingConstraint.Equals(other.HeatingConstraint) &&
				   TempC == other.TempC &&
				   TargetTempC == other.TargetTempC &&
				   NozzleNumber == other.NozzleNumber;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Number, HeatingConstraint, TempC, TargetTempC, NozzleNumber);
		}
	}
}
