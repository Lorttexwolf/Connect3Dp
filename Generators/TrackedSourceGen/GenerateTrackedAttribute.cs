using System;

namespace TrackedSourceGen
{
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	public sealed class GenerateTrackedAttribute : Attribute { }
}
