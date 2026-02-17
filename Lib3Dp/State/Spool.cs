using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	public record struct Spool(
		int Number,
		Material Material,
		int? GramsMaximum,
		int? GramsRemaining);
}
