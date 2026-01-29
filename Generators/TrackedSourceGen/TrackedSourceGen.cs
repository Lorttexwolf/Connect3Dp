using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace TrackedSourceGen
{
	[Generator]
	public class TrackedSourceGen : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var classSymbols = GetClassesWithAttributes(context);
			var compilationAndClasses = context.CompilationProvider.Combine(classSymbols.Collect());

			context.RegisterSourceOutput(compilationAndClasses, GenerateTrackedClasses);
		}

		private static IncrementalValuesProvider<INamedTypeSymbol> GetClassesWithAttributes(
			IncrementalGeneratorInitializationContext context)
		{
			return context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: IsClassWithAttributes,
					transform: GetClassSymbol
				)
				.Where(symbol => symbol != null)!;
		}

		private static bool IsClassWithAttributes(SyntaxNode node, CancellationToken _)
		{
			return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
		}

		private static INamedTypeSymbol? GetClassSymbol(GeneratorSyntaxContext context, CancellationToken _)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
		}

		private static void GenerateTrackedClasses(
			SourceProductionContext context,
			(Compilation Compilation, ImmutableArray<INamedTypeSymbol> Classes) source)
		{
			var (compilation, classes) = source;

			if (classes.IsDefaultOrEmpty)
				return;

			var attributeSymbol = compilation.GetTypeByMetadataName("TrackedSourceGen.GenerateTrackedAttribute");
			if (attributeSymbol == null)
				return;

			var generator = new TrackedClassGenerator(compilation, attributeSymbol);

			foreach (var classSymbol in classes.Distinct(SymbolEqualityComparer.Default))
			{
				if (classSymbol is not INamedTypeSymbol namedType)
					continue;

				if (!HasGenerateTrackedAttribute(namedType, attributeSymbol))
					continue;

				var sourceCode = generator.Generate(namedType);
				if (sourceCode != null)
				{
					context.AddSource(
						$"Tracked{namedType.Name}.g.cs",
						SourceText.From(sourceCode, Encoding.UTF8)
					);
				}
			}
		}

		private static bool HasGenerateTrackedAttribute(
			INamedTypeSymbol typeSymbol,
			INamedTypeSymbol attributeSymbol)
		{
			return typeSymbol.GetAttributes().Any(attr =>
				SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol)
			);
		}
	}

	internal class TrackedClassGenerator
	{
		private readonly Compilation _compilation;
		private readonly INamedTypeSymbol _attributeSymbol;
		private readonly TypeResolver _collectionResolver;

		public TrackedClassGenerator(Compilation compilation, INamedTypeSymbol attributeSymbol)
		{
			_compilation = compilation;
			_attributeSymbol = attributeSymbol;
			_collectionResolver = new TypeResolver(compilation);
		}

		public string? Generate(INamedTypeSymbol typeSymbol)
		{
			var properties = GetPublicInstanceProperties(typeSymbol);
			if (!properties.Any())
				return null;

			var builder = new TrackedClassBuilder(
				typeSymbol,
				properties,
				_collectionResolver
			);

			return builder.Build();
		}

		private static List<IPropertySymbol> GetPublicInstanceProperties(INamedTypeSymbol typeSymbol)
		{
			return typeSymbol.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => !p.IsStatic &&
						   p.DeclaredAccessibility == Accessibility.Public &&
						   p.GetMethod != null)
				.ToList();
		}
	}

	internal class TypeResolver
	{
		private readonly INamedTypeSymbol? _iDictionary;
		private readonly INamedTypeSymbol? _iCollection;
		private readonly INamedTypeSymbol? _iReadOnlyList;
		private readonly INamedTypeSymbol? _iReadOnlyDictionary;

		public TypeResolver(Compilation compilation)
		{
			_iDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
			_iCollection = compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
			_iReadOnlyList = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
			_iReadOnlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
		}

		public string GetTrackedType(ITypeSymbol propertyType)
		{
			// Check dictionary types (mutable and read-only)
			if (IsDictionaryType(propertyType, out var dictionaryArgs))
			{
				return $"TrackedDictionary<{dictionaryArgs}>";
			}

			// Check list types (mutable and read-only)
			if (IsCollectionType(propertyType, out var listArgs))
			{
				return $"TrackedCollection<{listArgs}>";
			}

			// Check for nested tracked types
			if (HasGenerateTrackedAttribute(propertyType))
			{
				return GetFullyQualifiedTrackedTypeName(propertyType);
			}

			if (propertyType.IsValueType)
			{
				return $"TrackedStruct<{GetFullyQualifiedName(propertyType)}>";
			}

			return $"TrackedValue<{GetFullyQualifiedName(propertyType)}>";
		}

		public string GetInitializer(IPropertySymbol property)
		{
			var trackedType = GetTrackedType(property.Type);

			if (HasGenerateTrackedAttribute(property.Type))
			{
				return $"new {trackedType}(_ref.{property.Name})";
			}

			return $"new {trackedType}(() => _ref.{property.Name})";
		}

		private bool IsDictionaryType(ITypeSymbol type, out string typeArguments)
		{
			typeArguments = string.Empty;

			if (_iDictionary != null && ImplementsInterface(type, _iDictionary))
			{
				typeArguments = GetDictionaryTypeArguments(type);
				return true;
			}

			if (_iReadOnlyDictionary != null && ImplementsInterface(type, _iReadOnlyDictionary))
			{
				typeArguments = GetDictionaryTypeArguments(type);
				return true;
			}

			return false;
		}

		private bool IsCollectionType(ITypeSymbol type, out string typeArguments)
		{
			typeArguments = string.Empty;

			if (_iCollection != null && ImplementsInterface(type, _iCollection))
			{
				typeArguments = GetListTypeArguments(type);
				return true;
			}

			if (_iReadOnlyList != null && ImplementsInterface(type, _iReadOnlyList))
			{
				typeArguments = GetListTypeArguments(type);
				return true;
			}

			return false;
		}

		private static string GetDictionaryTypeArguments(ITypeSymbol type)
		{
			if (type is INamedTypeSymbol { TypeArguments.Length: 2 } namedType)
			{
				var keyType = GetFullyQualifiedName(namedType.TypeArguments[0]);
				var valueType = GetFullyQualifiedName(namedType.TypeArguments[1]);
				return $"{keyType}, {valueType}";
			}

			return "object, object";
		}

		private static string GetListTypeArguments(ITypeSymbol type)
		{
			if (type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType)
			{
				return GetFullyQualifiedName(namedType.TypeArguments[0]);
			}

			return "object";
		}

		private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol interfaceSymbol)
		{
			if (type is not INamedTypeSymbol namedType)
				return false;

			// Check if type directly is the interface
			if (namedType.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default))
				return true;

			// Check all implemented interfaces
			return namedType.AllInterfaces.Any(i =>
				i.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default)
			);
		}

		public static bool HasGenerateTrackedAttribute(ITypeSymbol type)
		{
			return type.GetAttributes()
				.Any(attr => attr.AttributeClass?.Name == "GenerateTrackedAttribute");
		}

		private static string GetFullyQualifiedTrackedTypeName(ITypeSymbol type)
		{
			var ns = type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return $"{ns}.Tracked{type.Name}";
		}

		private static string GetFullyQualifiedName(ITypeSymbol type)
		{
			return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		}
	}

	internal class TrackedClassBuilder
	{
		private readonly INamedTypeSymbol _originalType;
		private readonly List<IPropertySymbol> _properties;
		private readonly TypeResolver _typeResolver;
		private readonly StringBuilder _builder;

		public TrackedClassBuilder(
			INamedTypeSymbol originalType,
			List<IPropertySymbol> properties,
			TypeResolver typeResolver)
		{
			_originalType = originalType;

			_properties = properties;

			var newProps =

			_typeResolver = typeResolver;
			_builder = new StringBuilder();
		}

		public string Build()
		{
			WriteUsings();
			WriteNamespaceStart();
			WriteClassStart();
			WriteReferenceField();
			WriteProperties();
			WriteConstructor();
			WriteMethods();
			WriteClassEnd();
			WriteNamespaceEnd();

			return _builder.ToString();
		}

		private void WriteUsings()
		{
			_builder.AppendLine("using System;");
			_builder.AppendLine("using System.Collections.Generic;");
			_builder.AppendLine("using TrackedSourceGen;");
			_builder.AppendLine();
		}

		private void WriteNamespaceStart()
		{
			var ns = _originalType.ContainingNamespace.ToDisplayString();
			_builder.AppendLine($"namespace {ns}");
			_builder.AppendLine("{");
		}

		private void WriteClassStart()
		{
			var trackedClassName = $"Tracked{_originalType.Name}";
			_builder.AppendLine($"\tinternal sealed partial class {trackedClassName}");
			_builder.AppendLine("\t{");
		}

		private void WriteReferenceField()
		{
			_builder.AppendLine($"\t\tprivate readonly {_originalType.Name} _ref;");
			_builder.AppendLine();
		}

		private void WriteProperties()
		{
			foreach (var property in _properties)
			{
				var trackedType = _typeResolver.GetTrackedType(property.Type);
				_builder.AppendLine($"\t\tpublic {trackedType} {property.Name} {{ get; }}");
			}
			_builder.AppendLine();

			_builder.AppendLine($"\t\tpublic bool HasChanged => {string.Join(" || ", _properties.Select(s => $"this.{s.Name}.HasChanged"))};");

			_builder.AppendLine();
		}

		private void WriteConstructor()
		{
			var trackedClassName = $"Tracked{_originalType.Name}";

			_builder.AppendLine($"\t\tpublic {trackedClassName}({_originalType.Name} refState)");
			_builder.AppendLine("\t\t{");
			_builder.AppendLine("\t\t\t_ref = refState ?? throw new ArgumentNullException(nameof(refState));");

			foreach (var property in _properties)
			{
				var initializer = _typeResolver.GetInitializer(property);
				_builder.AppendLine($"\t\t\t{property.Name} = {initializer};");
			}

			_builder.AppendLine("\t\t}");
		}

		private void WriteMethods()
		{
			_builder.AppendLine("\t\tpublic void ViewAll()");
			_builder.AppendLine("\t\t{");

			foreach (var property in _properties)
			{
				// Go through each and clear it.
				if (TypeResolver.HasGenerateTrackedAttribute(property.Type))
				{
					_builder.AppendLine($"\t\t\tthis.{property.Name}.ViewAll();");
				}
				else
				{
					_builder.AppendLine($"\t\t\tthis.{property.Name}.View();");
				}
			}

			_builder.AppendLine("\t\t}");
			_builder.AppendLine();
		}

		private void WriteClassEnd()
		{
			_builder.AppendLine("\t}");
		}

		private void WriteNamespaceEnd()
		{
			_builder.AppendLine("}");
		}
	}
}
