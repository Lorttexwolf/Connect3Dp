using Microsoft.CodeAnalysis;
using System.Linq;

namespace PartialBuilderSourceGen.Types
{
	internal record ClassOrStructureToUse
	{
		private readonly Context Context;
		public INamedTypeSymbol Base { get; }
		public PropertyToUse[] Properties { get; }

		public readonly string UpdaterName, ChangesName;
		public readonly bool IsClass, IsStruct;

		public readonly PropertyToUse? DictKeyProp;

		public ClassOrStructureToUse(INamedTypeSymbol symbol, Context context)
		{
			Context = context;
			Base = symbol;

			Properties = symbol.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility is Accessibility.Public && !p.IsReadOnly)
				.Select(p => new PropertyToUse(p, context))
				.ToArray();

			DictKeyProp = Properties.FirstOrDefault(p => p.HasDictKeyAttribute);

			UpdaterName = Constants.FormatAsUpdater(symbol);
			ChangesName = Constants.FormatAsChanges(symbol);

			IsClass = symbol.IsReferenceType;
			IsStruct = symbol.IsValueType;
		}
		public ClassOrStructureToUse(in TypeToUse symbolToUse, Context context) : this((INamedTypeSymbol)symbolToUse.Base, context) { }
	}
}
