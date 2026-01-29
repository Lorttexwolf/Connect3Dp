using System;

namespace PartialBuilderSourceGen
{
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class GeneratePartialBuilderAttribute : Attribute { }
}
