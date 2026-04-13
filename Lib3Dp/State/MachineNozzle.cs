using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{

	[GeneratePartialBuilder]
	public record struct MachineNozzle(
		[property: PartialBuilderDictKey] int Number, 
		double Diameter);
}
