using Microsoft.CodeAnalysis;
using PartialBuilderSourceGen.Attributes;
using PartialBuilderSourceGen.Extensions;
using System;
using System.Linq;
using System.Collections.Concurrent;

namespace PartialBuilderSourceGen.Types
{
	public record class TypeToUse
	{
		private static readonly ConcurrentDictionary<string, TypeToUse> Types = new(StringComparer.Ordinal);

		public readonly ITypeSymbol Base;
		public readonly bool IsValue;
		public readonly bool IsRecord;
		public readonly bool IsClass;
		public readonly bool IsReadOnly;
		public readonly bool IsCloneable;

		public readonly bool IsDictionary;
		private readonly ITypeSymbol? DictionaryKeyType;
		private readonly TypeToUse? DictionaryValueType;

		public readonly bool IsSet;
		private readonly TypeToUse? SetValueType;

		public readonly bool HasDictKeyAttrib;

		public readonly bool HasGenerateUpdaterAttrib;
		private ClassOrStructureToUse? UpdaterType;

		private TypeToUse(ITypeSymbol typeSymbol)
		{
			Base = typeSymbol;
			IsValue = typeSymbol.IsValueType;
			IsRecord = typeSymbol.IsRecord; // fixed: detect record properly
			IsClass = typeSymbol.IsReferenceType;
			IsReadOnly = typeSymbol.IsReadOnly;

			var methodsOfSymbol = typeSymbol.GetMembers().OfType<IMethodSymbol>();

			HasGenerateUpdaterAttrib = typeSymbol.HasAttribute(typeof(GeneratePartialBuilderAttribute).Name);
			HasDictKeyAttrib = typeSymbol.HasAttribute(typeof(PartialBuilderDictKeyAttribute).Name);

			var cloneMethod = methodsOfSymbol.FirstOrDefault(m => m.Name == "Clone" && m.Parameters.Length == 0);
			IsCloneable = cloneMethod is not null;

			if (typeSymbol is INamedTypeSymbol nD && nD.Name == "Dictionary" && nD.TypeArguments.Length == 2)
			{
				IsDictionary = true;
				DictionaryKeyType = nD.TypeArguments[0];
				DictionaryValueType = GetOrCreate((INamedTypeSymbol)nD.TypeArguments[1]);
			}
			else IsDictionary = false;

			if (typeSymbol is INamedTypeSymbol nS && (nS.Name == "HashSet" || nS.Name == "ISet" || nS.Name == "ICollection") && nS.TypeArguments.Length == 1)
			{
				IsSet = true;
				SetValueType = GetOrCreate((INamedTypeSymbol)nS.TypeArguments[0]);
			}
			else IsSet = false;
		}

		public static TypeToUse GetOrCreate(ITypeSymbol typeSymbol)
		{
			var key = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

			return Types.GetOrAdd(key, _ => new TypeToUse(typeSymbol));
		}

		public override string ToString()
		{
			return this.Base.ToString();
		}

		public bool TryIfDictionary(out ITypeSymbol? keyType, out TypeToUse? valueType)
		{
			keyType = null;
			valueType = null;
			if (!IsDictionary) return false;
			keyType = DictionaryKeyType;
			valueType = DictionaryValueType;
			return true;
		}

		public bool TryIfSet(out TypeToUse? valueType)
		{
			valueType = null;
			if (!IsSet) return false;
			valueType = SetValueType;
			return true;
		}

		public bool TryIfUpdater(out ClassOrStructureToUse? updaterToUse)
		{
			// TODO: Use some sort of context instead of regenerating this class many times.

			if (!this.HasGenerateUpdaterAttrib)
			{
				updaterToUse = null;
				return false;
			}

			this.UpdaterType ??= new ClassOrStructureToUse(this);

			updaterToUse = this.UpdaterType;

			return true;
		}
	}
}
