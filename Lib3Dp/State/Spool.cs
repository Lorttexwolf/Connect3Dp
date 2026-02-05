using PartialBuilderSourceGen;
using TrackedSourceGen;

namespace Lib3Dp.State
{

	[GeneratePartialBuilder]
	public class Spool : IReadOnlySpool, IEquatable<Spool?>
	{
		[PartialBuilderDictKey]
		public required int Number { get; set; }
		public required Material Material { get; set; }

		public int? GramsMaximum { get; set; }
		public int? GramsRemaining { get; set; }

		public override bool Equals(object? obj)
		{
			return Equals(obj as Spool);
		}

		public bool Equals(Spool? other)
		{
			return other is not null &&
				   Number == other.Number &&
				   EqualityComparer<Material>.Default.Equals(Material, other.Material) &&
				   GramsMaximum == other.GramsMaximum &&
				   GramsRemaining == other.GramsRemaining;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Number, Material, GramsMaximum, GramsRemaining);
		}

		public static bool operator ==(Spool? left, Spool? right)
		{
			return EqualityComparer<Spool>.Default.Equals(left, right);
		}

		public static bool operator !=(Spool? left, Spool? right)
		{
			return !(left == right);
		}
	}
}
