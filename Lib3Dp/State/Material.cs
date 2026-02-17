using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{
	/// <param name="Name">PLA, PETG, PPS-CF, PA-GF</param>
	/// <param name="Color"></param>
	/// <param name="FProfileIDX">Filament_id of the JSON filament profiles on Orca Slicer / BambuLab Studio.</param>
	[GeneratePartialBuilder]
	public record struct Material(string Name, MaterialColor Color, string? FProfileIDX)
	{
		public readonly bool IsSimilar(Material other)
		{
			return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Color.IsSimilarTo(other.Color, out _);
		}
	}
}
