using Microsoft.CodeAnalysis;
using PartialBuilderSourceGen.Attributes;
using PartialBuilderSourceGen.Extensions;
using System;

namespace PartialBuilderSourceGen.Types
{
	public record class PropertyToUse
	{
		public readonly IPropertySymbol Base;
		public readonly TypeToUse Type;
		public readonly bool IsNullable;
		/// <summary>
		/// Is required when TryCreate() is invoked.
		/// </summary>
		public readonly bool IsRequiredToCreate;
		public readonly bool HasDictKeyAttribute;

		public PropertyToUse(IPropertySymbol property)
		{
			Base = property;

			Type = TypeToUse.GetOrCreate(property.Type);

			IsNullable = property.IsNullable();
			IsRequiredToCreate = !property.IsNullable();
			HasDictKeyAttribute = property.Type.HasAttribute(typeof(PartialBuilderDictKeyAttribute).Name);
		}

		public override string ToString()
		{
			return this.Base.ToString();
		}
	}
}
