using System;

namespace PartialBuilderSourceGen.Attributes
{
	/// <summary>
	/// Marks a property to be a key when it's used in a dictionary and will be automatically included during initialization of the update action. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class PartialBuilderDictKeyAttribute : Attribute { };
}
