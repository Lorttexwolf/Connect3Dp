using Microsoft.CodeAnalysis;
using System.Linq;

namespace PartialBuilderSourceGen.Types
{
	public record ClassOrStructureToUse
	{
		public INamedTypeSymbol Base { get; }
		public PropertyToUse[] Properties { get; }

		public readonly string UpdaterName, ChangesName;
		public readonly bool IsClass, IsStruct;

		public readonly PropertyToUse? DictKeyProp;

		public ClassOrStructureToUse(INamedTypeSymbol symbol)
		{
			Base = symbol;

			Properties = symbol.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility is Accessibility.Public && !p.IsReadOnly)
				.Select(p => new PropertyToUse(p))
				.ToArray();

			DictKeyProp = Properties.FirstOrDefault(p => p.HasDictKeyAttribute);

			UpdaterName = Constants.FormatAsUpdater(symbol);
			ChangesName = Constants.FormatAsChanges(symbol);

			IsClass = symbol.IsReferenceType;
			IsStruct = symbol.IsValueType;
		}
		public ClassOrStructureToUse(in TypeToUse symbolToUse) : this((INamedTypeSymbol)symbolToUse.Base) { }
	}
}
