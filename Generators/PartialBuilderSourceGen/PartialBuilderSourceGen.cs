using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;

namespace PartialBuilderSourceGen
{
	[Generator]
	public sealed class PartialBuilderSourceGen : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var classes = context.SyntaxProvider
				.ForAttributeWithMetadataName(
					typeof(GeneratePartialBuilderAttribute).FullName!,
					static (n, _) => n is ClassDeclarationSyntax,
					static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

			context.RegisterSourceOutput(classes, static (ctx, type) =>
			{
				var source = UpdateEmitter.Emit(type);
				ctx.AddSource($"{type.Name}.Update.g.cs", source);
			});
		}
	}

	internal static class UpdateEmitter
	{
		public static string Emit(INamedTypeSymbol type)
		{
			var ns = type.ContainingNamespace.IsGlobalNamespace
				? null
				: type.ContainingNamespace.ToDisplayString();

			var updateName = type.Name + "Update";
			var sb = new StringBuilder();

			sb.AppendLine("#nullable enable");

			if (ns != null)
			{
				sb.AppendLine($"namespace {ns};");
				sb.AppendLine();
			}

			EmitUpdateClass(sb, type, updateName);
			EmitAppendUpdate(sb, type, updateName);

			return sb.ToString();
		}

		private static void EmitUpdateClass(StringBuilder sb, INamedTypeSymbol type, string updateName)
		{
			sb.AppendLine($"{type.DeclaredAccessibility.ToString().ToLower()} sealed class {updateName}");
			sb.AppendLine("{");

			var props = type.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility == Accessibility.Public);

			foreach (var p in props)
				EmitUpdateBacking(sb, p);

			sb.AppendLine();

			foreach (var p in props)
				EmitUpdateMethods(sb, p, updateName);

			EmitTryCreate(sb, type);

		}

		private static void EmitUpdateBacking(StringBuilder sb, IPropertySymbol p)
		{
			#region Dictionary

			{
				if (TypeHelpers.IsDictionary(p.Type, out var key, out var value) && p.Type is INamedTypeSymbol dict)
				{
					if (HasUpdater(dict.TypeArguments[1], out var updaterName))
					{
						sb.AppendLine($"    public Dictionary<{key}, {updaterName}>? {p.Name}ToSet {{ get; private set; }} = [];");
					}
					else
					{
						sb.AppendLine($"    public Dictionary<{key}, {value}>? {p.Name}ToSet {{ get; private set; }} = [];");
					}

					sb.AppendLine($"    public HashSet<{key}>? {p.Name}ToRemove {{ get; private set; }} = [];"); // Removals via key.

					return;
				}
			}

			#endregion

			#region Set<T>

			{
				if (TypeHelpers.IsSet(p.Type, out var elem) && p.Type is INamedTypeSymbol set)
				{
					if (HasUpdater(set.TypeArguments[0], out var setUpdater))
					{
						sb.AppendLine($"    public HashSet<{setUpdater}>? {p.Name}Updates {{ get; private set; }} = [];");
					}
					else
					{
						sb.AppendLine($"    public HashSet<{elem}>? {p.Name}ToSet {{ get; private set; }} = [];");
						sb.AppendLine($"    public HashSet<{elem}>? {p.Name}ToRemove {{ get; private set; }} = [];");
					}
					return;
				}
			}

			#endregion

			#region Classes, structs, and Nullables

			if (!p.Type.IsValueType && HasUpdater(p.Type, out var updater))
			{
				// Class, and is an updater.

				sb.AppendLine($"    public {updater}? {p.Name} {{ get; private set; }}");
			}
			else if (p.Type.IsValueType)
			{
				// Nullable

				if (p.Type.NullableAnnotation == NullableAnnotation.Annotated)
				{
					sb.AppendLine($"    public {p.Type} {p.Name} {{ get; private set; }}"); // ? is already included.
				}
				else
				{
					sb.AppendLine($"    public {p.Type}? {p.Name} {{ get; private set; }}");
				}

			}
			else
			{
				// Numbers and etc.

				sb.AppendLine($"    public {p.Type} {p.Name} {{ get; private set; }}");
			}

			sb.AppendLine($"    public bool {p.Name}IsSet {{ get; private set; }}");

			#endregion
		}

		private static void EmitUpdateMethods(StringBuilder sb, IPropertySymbol p, string updateName)
		{
			bool hasUpdater = HasUpdater(p.Type, out var updaterName);

			#region Dictionary

			if (TypeHelpers.IsDictionary(p.Type, out var key, out _) && p.Type is INamedTypeSymbol dict)
			{
				hasUpdater = HasUpdater(dict.TypeArguments[1], out updaterName);

				if (hasUpdater)
				{
					sb.AppendLine($$"""
                    public {{updateName}} Update{{p.Name}}({{key}} key, Action<{{updaterName}}> configure)
                    {
                        if (!{{p.Name}}ToSet.TryGetValue(key, out var update))
                        {
                            update = new {{updaterName}}();
                            {{p.Name}}ToSet[key] = update;
                        }

                        configure(update);
                        return this;
                    }
                    """);
				}
				else
				{
					sb.AppendLine($$"""
                    public {{updateName}} Set{{p.Name}}({{key}} key, {{dict.TypeArguments[1].Name}} val)
                    {
                        {{p.Name}}ToSet ??= new Dictionary<{{key}}, {{dict.TypeArguments[1].Name}}>();

                        {{p.Name}}ToSet[key] = val;

                        return this;
                    }
                    """);

					sb.AppendLine($$"""
                    public {{updateName}} Remove{{p.Name}}({{key}} key, {{dict.TypeArguments[1].Name}} val)
                    {
                        {{p.Name}}ToRemove ??= new HashSet<{{key}}>();

                        {{p.Name}}ToRemove.Add(key);

                        return this;
                    }
                    """);
				}

				return;
			}

			#endregion

			#region Set

			if (TypeHelpers.IsSet(p.Type, out _) && p.Type is INamedTypeSymbol set)
			{
				hasUpdater = HasUpdater(set.TypeArguments[0], out updaterName);

				var valType = set.TypeArguments[0];
				var valTypeName = valType.Name;
				var pName = p.Name;

				if (hasUpdater)
				{
					sb.AppendLine($$"""
                    public {{updateName}} Update{{p.Name}}(Action<{{updaterName}}> configure)
                    {
                        if (configure == null)
                            throw new ArgumentNullException(nameof(configure));

                        {{p.Name}}Updates ??= new HashSet<{{updaterName}}>();
                        var update = new {{updaterName}}();
                        configure(update);
                        {{p.Name}}Updates.Add(update);
                        return this;
                    }
                    """);
				}
				else
				{
					sb.AppendLine($$"""
                    public {{updateName}} Set{{pName}}({{valTypeName}} {{pName.ToLowerInvariant()}})
                    {
                        {{p.Name}}ToSet ??= new HashSet<{{valTypeName}}>();

                        {{p.Name}}ToSet.Add({{pName.ToLowerInvariant()}});
                        return this;
                    }
                    """);

					sb.AppendLine($$"""
                    public {{updateName}} Remove{{pName}}({{valTypeName}} {{pName.ToLowerInvariant()}})
                    {
                        {{p.Name}}ToRemove ??= new HashSet<{{valTypeName}}>();

                        {{p.Name}}ToRemove.Add({{pName.ToLowerInvariant()}});
                        return this;
                    }
                    """);
				}

				return;
			}

			#endregion

			#region Regular

			{
				if (hasUpdater)
				{
					sb.AppendLine($$"""

                    public {{updateName}} Update{{p.Name}}(Action<{{updaterName}}> configure)
                    {
                        {{p.Name}} ??= new {{updaterName}}();
                        configure({{p.Name}});
                        return this;
                    }

                    """);
				}
				else
				{
					sb.AppendLine($$"""

                    public {{updateName}} Set{{p.Name}}({{(p.IsNullable() ? p.Type : $"{p.Type}?")}} valueToSet)
                    {
                        {{p.Name}}IsSet = true;
                        {{p.Name}} = valueToSet;

                        return this;
                    }

                    """);
				}

				// Removal

				sb.AppendLine($$"""

                public {{updateName}} Remove{{p.Name}}()
                {
                    {{p.Name}}IsSet = true;
                    {{p.Name}} = null;
                    return this;
                }

                """);


			}

			//if (!p.Type.IsValueType && hasUpdater)
			//{
			//    sb.AppendLine($$"""
			//        public {{updateName}} Update{{p.Name}}(Action<{{updaterName}}> configure)
			//        {
			//            if (configure == null)
			//                throw new ArgumentNullException(nameof(configure));

			//            {{p.Name}}Update ??= new {{updaterName}}();
			//            configure({{p.Name}}Update);
			//            return this;
			//        }
			//        """);
			//}

			//if (hasUpdater)
			//{
			//    if (p.NullableAnnotation.HasFlag(NullableAnnotation.Annotated) && !hasUpdater)
			//    {
			//        sb.AppendLine($$"""
			//        public {{updateName}} Remove{{p.Name}}()
			//        {
			//            {{p.Name}}IsSet = true;
			//            {{p.Name}} = null;
			//            return this;
			//        }

			//        """);
			//    }
			//}
			//else
			//{
			//    // Fallback setter
			//    sb.AppendLine($"    public {updateName} Set{p.Name}({p.Type} value)");
			//    sb.AppendLine("    {");

			//    if (p.Type.IsValueType && !p.NullableAnnotation.HasFlag(NullableAnnotation.Annotated))
			//        sb.AppendLine($"        {p.Name} = value;");
			//    else
			//    {
			//        sb.AppendLine($"        {p.Name}IsSet = true;");
			//        sb.AppendLine($"        {p.Name} = value;");
			//    }

			//    sb.AppendLine("        return this;");
			//    sb.AppendLine("    }");
			//}

			#endregion
		}

		private static void EmitTryCreate(StringBuilder sb, INamedTypeSymbol type)
		{
			var requiredProps = type.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility == Accessibility.Public && p.IsRequired)
				.ToArray();

			sb.AppendLine();
			sb.AppendLine($"    public bool TryCreate(out {type.Name}? result)");
			sb.AppendLine("    {");
			sb.AppendLine("        result = null;");

			foreach (var p in requiredProps)
			{
				sb.AppendLine($"        if (!{p.Name}IsSet) return false;");
			}

			sb.AppendLine();
			sb.AppendLine($"        result = new {type.Name}()");
			sb.AppendLine("        {");

			for (int i = 0; i < requiredProps.Length; i++)
			{
				var p = requiredProps[i];

				sb.Append($"            {p.Name} = this.{p.Name}");


				if (p.Type.IsValueType)
					sb.Append(".Value");

				if (i < requiredProps.Length - 1)
					sb.Append(",");

				sb.AppendLine();
			}

			sb.AppendLine("        };");
			sb.AppendLine();
			sb.AppendLine("        this.AppendUpdate(result);");
			sb.AppendLine("        return true;");
			sb.AppendLine("    }");
		}

		private static void EmitAppendUpdate(StringBuilder sb, INamedTypeSymbol type, string updateName)
		{
			var inName = type.Name.ToLowerInvariant();

			sb.AppendLine($"    public void AppendUpdate({type.Name} {inName})");
			sb.AppendLine("    {");

			var props = type.GetMembers()
				.OfType<IPropertySymbol>()
				.Where(p => p.DeclaredAccessibility == Accessibility.Public);

			foreach (var p in props)
			{
				if (TypeHelpers.IsDictionary(p.Type, out _, out _) && p.Type is INamedTypeSymbol dict && HasUpdater(dict.TypeArguments[1], out _))
				{

					sb.AppendLine($$"""


                    if (this.{{p.Name}}ToSet != null || this.{{p.Name}}ToRemove != null)
                    {
                        if (this.{{p.Name}}ToSet != null)
                        {
                            foreach (var kvp in this.{{p.Name}}ToSet)
                            {
                                if ({{inName}}.{{p.Name}}.TryGetValue(kvp.Key, out var existing))
                                {
                                    kvp.Value.AppendUpdate(existing);
                                }
                                else if (kvp.Value.TryCreate(out var created))
                                {
                                    {{inName}}.{{p.Name}}[kvp.Key] = created;
                                }
                            }
                        }

                        if (this.{{p.Name}}ToRemove != null)
                        {
                            foreach (var valueToRemove in this.{{p.Name}}ToRemove)
                            {
                                {{inName}}.{{p.Name}}.Remove(valueToRemove);
                            }
                        }
                    }

                    """);

					continue;

					//sb.AppendLine($"        if ({inName}.{p.Name} != null)");
					//sb.AppendLine($"            foreach (var kv in this.{p.Name}ToSet)");
					//sb.AppendLine($"                if ({inName}.{p.Name}.TryGetValue(kv.Key, out var existing)) kv.Value.AppendUpdate(existing);");
					//sb.AppendLine($"                else if (kv.Value.TryCreate(out var created)) {inName}.{p.Name}[kv.Key] = created;");
				}

				if (TypeHelpers.IsSet(p.Type, out _) && p.Type is INamedTypeSymbol set && HasUpdater(set.TypeArguments[0], out _))
				{
					sb.AppendLine($"        if (this.{p.Name}ToSet != null)");
					sb.AppendLine($"            foreach (var u in this.{p.Name}ToSet)");
					sb.AppendLine($"                if (u.TryCreate(out var created))");
					sb.AppendLine($"                    {inName}.{p.Name}.Add(created);");

					continue;
				}

				if (HasUpdater(p.Type, out _))
				{
					sb.AppendLine($"        if (this.{p.Name} != null)");
					sb.AppendLine($"            if ({inName}.{p.Name} != null)");
					sb.AppendLine($"                this.{p.Name}.AppendUpdate({inName}.{p.Name});");
					sb.AppendLine($"            else if (this.{p.Name}.TryCreate(out var created))");
					sb.AppendLine($"                {inName}.{p.Name} = created;");

					continue;
				}

				if (TypeHelpers.IsDictionary(p.Type, out var key, out _))
				{
					sb.AppendLine($"        if (this.{p.Name}ToSet != null)");
					sb.AppendLine($"            foreach (var kv in this.{p.Name}ToSet)");
					sb.AppendLine($"                {inName}.{p.Name}[kv.Key] = kv.Value;");

					sb.AppendLine($"        if (this.{p.Name}ToRemove != null)");
					sb.AppendLine($"            foreach (var k in this.{p.Name}ToRemove)");
					sb.AppendLine($"                {inName}.{p.Name}.Remove(k);");
				}
				else if (TypeHelpers.IsSet(p.Type, out _))
				{
					sb.AppendLine($"        if (this.{p.Name}ToSet != null)");
					sb.AppendLine($"            {inName}.{p.Name}.UnionWith(this.{p.Name}ToSet);");

					sb.AppendLine($"        if (this.{p.Name}ToRemove != null)");
					sb.AppendLine($"            {inName}.{p.Name}.ExceptWith(this.{p.Name}ToRemove);");
				}
				else
				{
					if (p.Type.IsValueType && !p.NullableAnnotation.HasFlag(NullableAnnotation.Annotated))
					{
						sb.AppendLine($"        if (this.{p.Name}IsSet) {inName}.{p.Name} = this.{p.Name}.Value;");
					}
					else
					{
						sb.AppendLine($"        if (this.{p.Name}IsSet) {inName}.{p.Name} = this.{p.Name};");
					}
				}
			}

			sb.AppendLine("    }");
			sb.AppendLine("}");
		}

		//private static void EmitAppendUpdatePartial(StringBuilder sb, INamedTypeSymbol type, string updateName)
		//{
		//    sb.AppendLine();
		//    sb.AppendLine($"{type.DeclaredAccessibility.ToString().ToLower()} partial class {updateName}");
		//    sb.AppendLine("{");
		//    sb.AppendLine($"    public void AppendUpdate({updateName} update)");
		//    sb.AppendLine("    {");

		//    var props = type.GetMembers()
		//        .OfType<IPropertySymbol>()
		//        .Where(p => p.DeclaredAccessibility == Accessibility.Public);

		//    foreach (var p in props)
		//    {
		//        if (TypeHelpers.IsDictionary(p.Type, out _, out _) &&
		//            p.Type is INamedTypeSymbol dict &&
		//            HasUpdater(dict.TypeArguments[1], out _))
		//        {
		//            sb.AppendLine($"        if (update.{p.Name}Updates != null)");
		//            sb.AppendLine($"            foreach (var kv in update.{p.Name}Updates)");
		//            sb.AppendLine($"                if ({p.Name}.TryGetValue(kv.Key, out var existing))");
		//            sb.AppendLine($"                    existing.AppendUpdate(kv.Value);");
		//            sb.AppendLine($"                else if (kv.Value.TryCreate(out var created))");
		//            sb.AppendLine($"                    {p.Name}[kv.Key] = created;");
		//        }

		//        if (TypeHelpers.IsSet(p.Type, out _) &&
		//            p.Type is INamedTypeSymbol set &&
		//            HasUpdater(set.TypeArguments[0], out _))
		//        {
		//            sb.AppendLine($"        if (update.{p.Name}Updates != null)");
		//            sb.AppendLine($"            foreach (var u in update.{p.Name}Updates)");
		//            sb.AppendLine($"                if (u.TryCreate(out var created))");
		//            sb.AppendLine($"                    {p.Name}.Add(created);");
		//        }

		//        if (!p.Type.IsValueType &&
		//            HasUpdater(p.Type, out _))
		//        {
		//            sb.AppendLine($"        if (update.{p.Name}Update != null)");
		//            sb.AppendLine($"            if ({p.Name} != null)");
		//            sb.AppendLine($"                {p.Name}.AppendUpdate(update.{p.Name}Update);");
		//            sb.AppendLine($"            else if (update.{p.Name}Update.TryCreate(out var created))");
		//            sb.AppendLine($"                {p.Name} = created;");
		//        }

		//        if (TypeHelpers.IsDictionary(p.Type, out var key, out _))
		//        {
		//            sb.AppendLine($"        if (update.{p.Name}ToSet != null)");
		//            sb.AppendLine($"            foreach (var kv in update.{p.Name}ToSet)");
		//            sb.AppendLine($"                {p.Name}[kv.Key] = kv.Value;");

		//            sb.AppendLine($"        if (update.{p.Name}ToRemove != null)");
		//            sb.AppendLine($"            foreach (var k in update.{p.Name}ToRemove)");
		//            sb.AppendLine($"                {p.Name}.Remove(k);");
		//        }
		//        else if (TypeHelpers.IsSet(p.Type, out _))
		//        {
		//            sb.AppendLine($"        if (update.{p.Name}ToAdd != null)");
		//            sb.AppendLine($"            {p.Name}.UnionWith(update.{p.Name}ToAdd);");

		//            sb.AppendLine($"        if (update.{p.Name}ToRemove != null)");
		//            sb.AppendLine($"            {p.Name}.ExceptWith(update.{p.Name}ToRemove);");
		//        }
		//        else
		//        {
		//            if (p.Type.IsValueType && !p.NullableAnnotation.HasFlag(NullableAnnotation.Annotated))
		//            {
		//                sb.AppendLine($"        if (update.{p.Name}.HasValue)");
		//                sb.AppendLine($"            {p.Name} = update.{p.Name}.Value;");
		//            }
		//            else
		//            {
		//                sb.AppendLine($"        if (update.{p.Name}Set)");
		//                sb.AppendLine($"            {p.Name} = update.{p.Name};");
		//            }
		//        }
		//    }

		//    sb.AppendLine("    }");
		//    sb.AppendLine("}");
		//}

		public static bool HasUpdater(ITypeSymbol type, out string updateTypeName)
		{
			updateTypeName = null!;
			if (type is not INamedTypeSymbol named) return false;

			foreach (var attr in named.GetAttributes())
			{
				if (attr.AttributeClass?.ToDisplayString()
					== typeof(GeneratePartialBuilderAttribute).FullName)
				{
					updateTypeName = named.Name + "Update";
					return true;
				}
			}

			return false;
		}
	}

	internal static class TypeHelpers
	{
		public static bool IsDictionary(ITypeSymbol type, out string key, out string value)
		{
			if (type is INamedTypeSymbol n && n.Name == "Dictionary" && n.TypeArguments.Length == 2)
			{
				key = n.TypeArguments[0].ToDisplayString();
				value = n.TypeArguments[1].ToDisplayString();
				return true;
			}
			key = value = null!;
			return false;
		}

		public static bool IsSet(ITypeSymbol type, out string element)
		{
			if (type is INamedTypeSymbol n &&
				n.Name is "HashSet" or "ISet" or "ICollection" &&
				n.TypeArguments.Length == 1)
			{
				element = n.TypeArguments[0].ToDisplayString();
				return true;
			}
			element = null!;
			return false;
		}
	}
}
