using Microsoft.CodeAnalysis;
using PartialBuilderSourceGen.Attributes;
using PartialBuilderSourceGen.Extensions;
using System;
using System.Linq;

namespace PartialBuilderSourceGen.Types
{
	internal record class PropertyToUse
	{
		private readonly Context Context;

		public readonly IPropertySymbol Base;
		public readonly TypeToUse Type;
		public readonly bool IsNullable;
		/// <summary>
		/// Is required when TryCreate() is invoked.
		/// </summary>
		public readonly bool IsRequiredToCreate;
		public readonly bool HasDictKeyAttribute;
		/// <summary>
		/// True when the property uses <c>init</c> — settable only during object construction.
		/// Such properties are included in TryCreate() object initializers but must be skipped
		/// in AppendUpdate() since they cannot be set after construction.
		/// </summary>
		public readonly bool IsInitOnly;

		public PropertyToUse(IPropertySymbol property, Context context)
		{
			Context = context;
			Base = property;

			Type = TypeToUse.GetOrCreate(property.Type, context);

			IsNullable = property.IsNullable();
			IsRequiredToCreate = !property.IsNullable();
			HasDictKeyAttribute = property.ContainsAttribute(context.DictKeyAttribSymbol);
			IsInitOnly = property.SetMethod?.IsInitOnly == true;
		}

		public override string ToString()
		{
			return this.Base.ToString();
		}
	}
}
