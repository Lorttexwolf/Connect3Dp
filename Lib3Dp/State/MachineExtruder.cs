using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	public record struct MachineExtruder(
		[property: PartialBuilderDictKey] int Number, 
		HeatingConstraints HeatingConstraint, 
		double TempC, 
		double? TargetTempC, 
		int? NozzleNumber, 
		SpoolLocation? LoadedSpool);
}
