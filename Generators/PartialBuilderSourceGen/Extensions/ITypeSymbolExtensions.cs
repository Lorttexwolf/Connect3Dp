using Microsoft.CodeAnalysis;
using System.Linq;

namespace PartialBuilderSourceGen.Extensions
{
	internal static class ITypeSymbolExtensions
	{
		public static bool IsNullable(this IPropertySymbol property)
		{
			// Nullable reference types (string?, MyClass?)
			if (property.NullableAnnotation == NullableAnnotation.Annotated)
				return true;

			// Nullable value types (int?, enum?, struct?)
			if (property.Type is INamedTypeSymbol named &&
				named.IsGenericType &&
				named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
				return true;

			return false;
		}

		public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
		{
			return symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));
		}
	}
}
