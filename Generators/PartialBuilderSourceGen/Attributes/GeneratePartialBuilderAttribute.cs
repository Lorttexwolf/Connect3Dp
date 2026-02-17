using System;

namespace PartialBuilderSourceGen.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class GeneratePartialBuilderAttribute : Attribute { }
}
