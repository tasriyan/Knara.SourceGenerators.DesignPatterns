using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenerators.DesignPatterns.Builder;

[Generator]
public class BuilderPatternGenerator : IIncrementalGenerator
{
    private const string NamespaceSource = "SourceGenerators.DesignPatterns.Builder";
    private const string AttributesSource = """
        using System;

        namespace SourceGenerators.DesignPatterns.Builder;

        [AttributeUsage(AttributeTargets.Class)]
        public class GenerateBuilderAttribute : Attribute
        {
            public bool ValidateOnBuild { get; set; } = true;
            public string? BuilderName { get; set; }
            public bool GenerateWithMethods { get; set; } = true;
            public bool GenerateFromMethod { get; set; } = true;
            public bool GenerateToBuilderMethod { get; set; } = true;
            public BuilderAccessibility Accessibility { get; set; } = BuilderAccessibility.Public;
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class BuilderPropertyAttribute : Attribute
        {
            public bool Required { get; set; }
            public string? ValidatorMethod { get; set; }
            public object? DefaultValue { get; set; }
            public bool IgnoreInBuilder { get; set; }
            public string? CustomSetterName { get; set; }
            public bool AllowNull { get; set; } = true;
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class BuilderCollectionAttribute : Attribute
        {
            public string? AddMethodName { get; set; }
            public string? AddRangeMethodName { get; set; }
            public bool GenerateClearMethod { get; set; } = true;
            public bool GenerateCountProperty { get; set; } = true;
        }

        public enum BuilderAccessibility
        {
            Public,
            Internal,
            Private
        }
        """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate the attributes as source code
        context.RegisterPostInitializationOutput(ctx => ctx
            .AddSource("BuilderAttributes.g.cs",
                SourceText.From(AttributesSource, Encoding.UTF8)));

        // Find all classes/records with [GenerateBuilder] attribute
        var builderCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        // Generate builders for each candidate
        context.RegisterSourceOutput(builderCandidates, static (spc, source) => Execute(source!, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax or RecordDeclarationSyntax;
    }

    static TypeToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;

        // Check if the type has the GenerateBuilder attribute
        foreach (var attributeList in typeDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is IMethodSymbol attributeSymbol)
                {
                    var attributeType = attributeSymbol.ContainingType;
                    if (attributeType.Name == "GenerateBuilderAttribute" &&
                        attributeType.ContainingNamespace?.ToDisplayString() == NamespaceSource)
                    {
                        return CreateTypeToGenerate(context, typeDeclaration);
                    }
                }
            }
        }

        return null;
    }

    static TypeToGenerate CreateTypeToGenerate(GeneratorSyntaxContext context, TypeDeclarationSyntax typeDeclaration)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null) return null;

        var properties = new List<PropertyToGenerate>();

        foreach (var member in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Include property if it has a setter OR has BuilderProperty attribute OR is a collection
            var hasBuilderAttribute = GetBuilderPropertyAttribute(member) != null;
            var isCollection = IsCollectionType(member.Type);
            var hasSetter = member.SetMethod != null;
        
            if (hasSetter || hasBuilderAttribute || isCollection)
            {
                var property = CreatePropertyToGenerate(member);
                if (property != null && !property.IgnoreInBuilder)
                {
                    properties.Add(property);
                }
            }
        }

        var builderAttribute = GetBuilderAttribute(typeSymbol);

        return new TypeToGenerate
        {
            Name = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace.ToDisplayString(),
            IsRecord = typeDeclaration is RecordDeclarationSyntax,
            Properties = properties,
            BuilderName = builderAttribute?.BuilderName ?? $"{typeSymbol.Name}Builder",
            ValidateOnBuild = builderAttribute?.ValidateOnBuild ?? true,
            GenerateWithMethods = builderAttribute?.GenerateWithMethods ?? true,
            GenerateFromMethod = builderAttribute?.GenerateFromMethod ?? true,
            Accessibility = builderAttribute?.Accessibility ?? BuilderAccessibility.Public
        };
    }
    
    static PropertyToGenerate? CreatePropertyToGenerate(IPropertySymbol propertySymbol)
    {
        var builderPropAttr = GetBuilderPropertyAttribute(propertySymbol);
        var collectionAttr = GetBuilderCollectionAttribute(propertySymbol);

        if (builderPropAttr?.IgnoreInBuilder == true) return null;

        var isCollection = IsCollectionType(propertySymbol.Type);
        var hasSetter = propertySymbol.SetMethod != null;

        return new PropertyToGenerate
        {
            Name = propertySymbol.Name,
            TypeName = propertySymbol.Type.ToDisplayString(),
            IsRequired = builderPropAttr?.Required ?? false,
            ValidatorMethod = builderPropAttr?.ValidatorMethod,
            DefaultValue = builderPropAttr?.DefaultValue?.ToString(),
            IgnoreInBuilder = builderPropAttr?.IgnoreInBuilder ?? false,
            CustomSetterName = builderPropAttr?.CustomSetterName,
            AllowNull = builderPropAttr?.AllowNull ?? true,
            IsCollection = isCollection,
            CollectionElementType = isCollection ? GetCollectionElementType(propertySymbol.Type) : null,
            AddMethodName = collectionAttr?.AddMethodName,
            GenerateClearMethod = collectionAttr?.GenerateClearMethod ?? true,
            GenerateCountProperty = collectionAttr?.GenerateCountProperty ?? true,
            HasSetter = hasSetter
        };
    }

    static bool IsDictionaryType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               namedType.ConstructedFrom?.ToDisplayString() == "System.Collections.Generic.Dictionary<TKey, TValue>";
    }
    
    static bool IsDictionaryType(string typeName)
    {
        return typeName.Contains("Dictionary<") || typeName.Contains("IDictionary<");
    }
    
    // Add helper method to detect if property has setter
    static bool HasSetter(PropertyToGenerate prop)
    {
        return prop.HasSetter;
    }
    
    static bool IsCollectionType(ITypeSymbol type)
    {
        // Exclude string and nullable types
        if (type.SpecialType == SpecialType.System_String)
            return false;

        // Exclude Dictionary types - they should be handled as scalar properties
        if (IsDictionaryType(type))
            return false;

        if (type.TypeKind == TypeKind.Array) return true;

        // Only treat as collection if implements IEnumerable and is not string or dictionary
        return type.AllInterfaces.Any(i =>
            (i.Name == "IEnumerable" ||
             i.Name == "ICollection" ||
             i.Name == "IList" ||
             i.Name == "IReadOnlyCollection" ||
             i.Name == "IReadOnlyList")
        ) && type.SpecialType != SpecialType.System_String;
    }

    static string? GetCollectionElementType(ITypeSymbol type)
    {
        if (IsDictionaryType(type) && type is INamedTypeSymbol dictType && dictType.TypeArguments.Length == 2)
            return $"KeyValuePair<{dictType.TypeArguments[0].ToDisplayString()}, {dictType.TypeArguments[1].ToDisplayString()}>";

        if (type is IArrayTypeSymbol arrayType)
            return arrayType.ElementType.ToDisplayString();

        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0].ToDisplayString();

        return "object";
    }

    static void Execute(TypeToGenerate typeToGenerate, SourceProductionContext context)
    {
        var sourceBuilder = new StringBuilder();

        // Generate the builder class
        sourceBuilder.AppendLine("// <auto-generated />");
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using System.Linq;");
        sourceBuilder.AppendLine();

        if (!string.IsNullOrEmpty(typeToGenerate.Namespace))
        {
            sourceBuilder.AppendLine($"namespace {typeToGenerate.Namespace}");
            sourceBuilder.AppendLine("{");
        }

        GenerateBuilderClass(sourceBuilder, typeToGenerate);
    
        // Generate extension methods at namespace level (not nested)
        GenerateExtensionMethods(sourceBuilder, typeToGenerate);

        if (!string.IsNullOrEmpty(typeToGenerate.Namespace))
        {
            sourceBuilder.AppendLine("}");
        }

        context.AddSource($"{typeToGenerate.BuilderName}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
    }

    static void GenerateBuilderClass(StringBuilder sb, TypeToGenerate type)
    {
        var accessibility = type.Accessibility.ToString().ToLower();

        sb.AppendLine($"    {accessibility} sealed class {type.BuilderName}");
        sb.AppendLine("    {");

        // Generate private fields
        GeneratePrivateFields(sb, type);

        // Generate constructor
        sb.AppendLine($"        private {type.BuilderName}() {{ }}");
        sb.AppendLine();

        // Generate Create method
        sb.AppendLine($"        public static {type.BuilderName} Create() => new();");
        sb.AppendLine();

        // Generate From method
        if (type.GenerateFromMethod)
        {
            GenerateFromMethod(sb, type);
        }

        // Generate With methods
        if (type.GenerateWithMethods)
        {
            GenerateWithMethods(sb, type);
        }

        // Generate collection methods
        GenerateCollectionMethods(sb, type);

        // Generate Build method
        GenerateBuildMethod(sb, type);

        // Generate validation methods
        GenerateValidationMethods(sb, type);

        sb.AppendLine("    }");
    }

    static void GeneratePrivateFields(StringBuilder sb, TypeToGenerate type)
    {
        foreach (var prop in type.Properties)
        {
            if (prop.IsCollection)
            {
                sb.AppendLine($"        private List<{prop.CollectionElementType}> _{CamelCase(prop.Name)} = new();");
            }
            else
            {
                var defaultValue = GetDefaultValueForType(prop);
                sb.AppendLine($"        private {prop.TypeName} _{CamelCase(prop.Name)}{defaultValue};");
            }

            if (prop.IsRequired)
            {
                sb.AppendLine($"        private bool _{CamelCase(prop.Name)}Set = false;");
            }
        }
        sb.AppendLine();
    }

    static void GenerateFromMethod(StringBuilder sb, TypeToGenerate type)
    {
        sb.AppendLine($"        public static {type.BuilderName} From({type.Name} source) => new()");
        sb.AppendLine("        {");

        foreach (var prop in type.Properties)
        {
            if (prop.IsCollection)
            {
                sb.AppendLine($"            _{CamelCase(prop.Name)} = source.{prop.Name}?.ToList() ?? new(),");
            }
            else
            {
                sb.AppendLine($"            _{CamelCase(prop.Name)} = source.{prop.Name},");
            }

            if (prop.IsRequired)
            {
                sb.AppendLine($"            _{CamelCase(prop.Name)}Set = true,");
            }
        }

        sb.AppendLine("        };");
        sb.AppendLine();
    }

    static void GenerateWithMethods(StringBuilder sb, TypeToGenerate type)
    {
        foreach (var prop in type.Properties.Where(p => !p.IsCollection))
        {
            if (IsDictionaryType(prop.TypeName))
            {
                // Generate dictionary-specific methods
                GenerateDictionaryMethods(sb, type, prop);
            }
            else
            {
                // Generate regular With method
                var methodName = prop.CustomSetterName ?? $"With{prop.Name}";
                var paramName = CamelCase(prop.Name);

                sb.AppendLine($"        public {type.BuilderName} {methodName}({prop.TypeName} {paramName})");
                sb.AppendLine("        {");

                // Add validation if specified
                if (!string.IsNullOrEmpty(prop.ValidatorMethod))
                {
                    sb.AppendLine($"            if (!{type.Name}.{prop.ValidatorMethod}({paramName}))");
                    sb.AppendLine($"                throw new ArgumentException(\"{prop.Name} validation failed\", nameof({paramName}));");
                    sb.AppendLine();
                }

                // Add null check if not allowed
                if (!prop.AllowNull && !prop.TypeName.EndsWith("?"))
                {
                    sb.AppendLine($"            if ({paramName} == null)");
                    sb.AppendLine($"                throw new ArgumentNullException(nameof({paramName}));");
                    sb.AppendLine();
                }

                sb.AppendLine($"            _{CamelCase(prop.Name)} = {paramName};");

                if (prop.IsRequired)
                {
                    sb.AppendLine($"            _{CamelCase(prop.Name)}Set = true;");
                }

                sb.AppendLine("            return this;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }
    }

    static void GenerateCollectionMethods(StringBuilder sb, TypeToGenerate type)
    {
        foreach (var prop in type.Properties.Where(p => p.IsCollection))
        {
            var addMethod = prop.AddMethodName ?? $"Add{prop.Name.TrimEnd('s')}";
            var paramName = CamelCase(prop.Name.TrimEnd('s'));

            // Add single item method
            sb.AppendLine($"        public {type.BuilderName} {addMethod}({prop.CollectionElementType} {paramName})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if ({paramName} == null) throw new ArgumentNullException(nameof({paramName}));");
            sb.AppendLine($"            _{CamelCase(prop.Name)}.Add({paramName});");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Add range method
            sb.AppendLine($"        public {type.BuilderName} Add{prop.Name}(IEnumerable<{prop.CollectionElementType}> {CamelCase(prop.Name)})");
            sb.AppendLine("        {");
            sb.AppendLine($"            if ({CamelCase(prop.Name)} == null) throw new ArgumentNullException(nameof({CamelCase(prop.Name)}));");
            sb.AppendLine($"            _{CamelCase(prop.Name)}.AddRange({CamelCase(prop.Name)});");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Clear method
            if (prop.GenerateClearMethod)
            {
                sb.AppendLine($"        public {type.BuilderName} Clear{prop.Name}()");
                sb.AppendLine("        {");
                sb.AppendLine($"            _{CamelCase(prop.Name)}.Clear();");
                sb.AppendLine("            return this;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Count property
            if (prop.GenerateCountProperty)
            {
                sb.AppendLine($"        public int {prop.Name}Count => _{CamelCase(prop.Name)}.Count;");
                sb.AppendLine();
            }
        }
    }

static void GenerateBuildMethod(StringBuilder sb, TypeToGenerate type)
{
    sb.AppendLine($"        public {type.Name} Build()");
    sb.AppendLine("        {");

    if (type.ValidateOnBuild)
    {
        var requiredProps = type.Properties.Where(p => p.IsRequired).ToList();
        if (requiredProps.Any())
        {
            foreach (var prop in requiredProps)
            {
                sb.AppendLine($"            if (!_{CamelCase(prop.Name)}Set)");
                sb.AppendLine($"                throw new InvalidOperationException(\"Required property '{prop.Name}' has not been set\");");
            }
            sb.AppendLine();
        }
    }

    // Check if properties have setters (init or set) - use object initializer
    // If only get properties - use constructor
    bool hasSettableProperties = type.Properties.Any(p => HasSetter(p));

    if (hasSettableProperties)
    {
        sb.AppendLine($"            return new {type.Name}");
        sb.AppendLine("            {");

        foreach (var prop in type.Properties)
        {
            if (prop.IsCollection)
            {
                if (prop.TypeName.Contains("ReadOnly"))
                {
                    sb.AppendLine($"                {prop.Name} = _{CamelCase(prop.Name)}.AsReadOnly(),");
                }
                else
                {
                    sb.AppendLine($"                {prop.Name} = _{CamelCase(prop.Name)}.ToList(),");
                }
            }
            else
            {
                sb.AppendLine($"                {prop.Name} = _{CamelCase(prop.Name)},");
            }
        }

        sb.AppendLine("            };");
    }
    else
    {
        // Use constructor for classes with only get properties
        sb.AppendLine($"            return new {type.Name}(");
        var parameters = type.Properties.Select(p =>
        {
            if (p.IsCollection)
            {
                if (p.TypeName.Contains("ReadOnly"))
                    return $"_{CamelCase(p.Name)}.ToList()";
                else
                    return $"_{CamelCase(p.Name)}.ToList()";
            }
            else
            {
                return $"_{CamelCase(p.Name)}";
            }
        });
        sb.AppendLine($"                {string.Join(",\n                ", parameters)}");
        sb.AppendLine("            );");
    }

    sb.AppendLine("        }");  // This closing bracket was missing!
    sb.AppendLine();
}

    static void GenerateValidationMethods(StringBuilder sb, TypeToGenerate type)
    {
        var requiredProps = type.Properties.Where(p => p.IsRequired).ToList();
        if (!requiredProps.Any()) return;

        sb.AppendLine("        public bool CanBuild() =>");
        var conditions = requiredProps.Select(p => $"_{CamelCase(p.Name)}Set");
        sb.AppendLine($"            {string.Join(" && ", conditions)};");
        sb.AppendLine();

        sb.AppendLine("        public IEnumerable<string> GetMissingRequiredProperties()");
        sb.AppendLine("        {");
        foreach (var prop in requiredProps)
        {
            sb.AppendLine($"            if (!_{CamelCase(prop.Name)}Set) yield return nameof({type.Name}.{prop.Name});");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    static void GenerateExtensionMethods(StringBuilder sb, TypeToGenerate type)
    {
        if (!type.GenerateFromMethod) return;

        // Close the builder class first
        sb.AppendLine();
    
        // Generate extension class at namespace level
        sb.AppendLine($"    public static class {type.Name}Extensions");
        sb.AppendLine("    {");
        sb.AppendLine($"        public static {type.BuilderName} ToBuilder(this {type.Name} source) =>");
        sb.AppendLine($"            {type.BuilderName}.From(source);");
        sb.AppendLine("    }");
    }
    
    static void GenerateDictionaryMethods(StringBuilder sb, TypeToGenerate type, PropertyToGenerate prop)
    {
        var paramName = CamelCase(prop.Name);
    
        // With method for entire dictionary
        sb.AppendLine($"        public {type.BuilderName} With{prop.Name}({prop.TypeName} {paramName})");
        sb.AppendLine("        {");
        sb.AppendLine($"            _{CamelCase(prop.Name)} = {paramName} ?? new();");
        if (prop.IsRequired)
        {
            sb.AppendLine($"            _{CamelCase(prop.Name)}Set = true;");
        }
        sb.AppendLine("            return this;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add key-value pair method
        sb.AppendLine($"        public {type.BuilderName} Add{prop.Name.TrimEnd('s')}(string key, string value)");
        sb.AppendLine("        {");
        sb.AppendLine($"            _{CamelCase(prop.Name)}[key] = value;");
        sb.AppendLine("            return this;");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Clear method
        sb.AppendLine($"        public {type.BuilderName} Clear{prop.Name}()");
        sb.AppendLine("        {");
        sb.AppendLine($"            _{CamelCase(prop.Name)}.Clear();");
        sb.AppendLine("            return this;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    // Helper methods
    static string CamelCase(string input) =>
        string.IsNullOrEmpty(input) ? input : char.ToLower(input[0]) + input.Substring(1);

    static string GetDefaultValueForType(PropertyToGenerate prop)
    {
        if (!string.IsNullOrEmpty(prop.DefaultValue))
            return $" = {prop.DefaultValue}";

        if (prop.TypeName.EndsWith("?") || prop.AllowNull)
            return "";

        return prop.TypeName switch
        {
            "string" => " = \"\"",
            "int" => " = 0",
            "bool" => " = false",
            "DateTime" => " = default",
            "TimeSpan" => " = default",
            _ => ""
        };
    }

    static BuilderPropertyAttribute? GetBuilderPropertyAttribute(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            string attributeName = attr.AttributeClass?.Name ?? string.Empty;
            if (attributeName == "BuilderPropertyAttribute" || attributeName == "BuilderProperty")
            {
                var result = new BuilderPropertyAttribute();

                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(BuilderPropertyAttribute.Required):
                            result.Required = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderPropertyAttribute.ValidatorMethod):
                            result.ValidatorMethod = (string?)namedArg.Value.Value;
                            break;
                        case nameof(BuilderPropertyAttribute.DefaultValue):
                            result.DefaultValue = namedArg.Value.Value;
                            break;
                        case nameof(BuilderPropertyAttribute.IgnoreInBuilder):
                            result.IgnoreInBuilder = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderPropertyAttribute.CustomSetterName):
                            result.CustomSetterName = (string?)namedArg.Value.Value;
                            break;
                        case nameof(BuilderPropertyAttribute.AllowNull):
                            result.AllowNull = (bool)namedArg.Value.Value!;
                            break;
                    }
                }

                return result;
            }
        }

        return null;
    }

    static BuilderCollectionAttribute? GetBuilderCollectionAttribute(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            string attributeName = attr.AttributeClass?.Name ?? string.Empty;
            if (attributeName == "BuilderCollectionAttribute" || attributeName == "BuilderCollection")
            {
                var result = new BuilderCollectionAttribute();

                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(BuilderCollectionAttribute.AddMethodName):
                            result.AddMethodName = (string?)namedArg.Value.Value;
                            break;
                        case nameof(BuilderCollectionAttribute.AddRangeMethodName):
                            result.AddRangeMethodName = (string?)namedArg.Value.Value;
                            break;
                        case nameof(BuilderCollectionAttribute.GenerateClearMethod):
                            result.GenerateClearMethod = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderCollectionAttribute.GenerateCountProperty):
                            result.GenerateCountProperty = (bool)namedArg.Value.Value!;
                            break;
                    }
                }

                return result;
            }
        }

        return null;
    }

    static BuilderAttribute? GetBuilderAttribute(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            string attributeName = attr.AttributeClass?.Name ?? string.Empty;
            if (attributeName == "GenerateBuilderAttribute" || attributeName == "GenerateBuilder")
            {
                var result = new BuilderAttribute();

                foreach (var namedArg in attr.NamedArguments)
                {
                    switch (namedArg.Key)
                    {
                        case nameof(BuilderAttribute.ValidateOnBuild):
                            result.ValidateOnBuild = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderAttribute.BuilderName):
                            result.BuilderName = (string?)namedArg.Value.Value;
                            break;
                        case nameof(BuilderAttribute.GenerateWithMethods):
                            result.GenerateWithMethods = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderAttribute.GenerateFromMethod):
                            result.GenerateFromMethod = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderAttribute.GenerateToBuilderMethod):
                            result.GenerateToBuilderMethod = (bool)namedArg.Value.Value!;
                            break;
                        case nameof(BuilderAttribute.Accessibility):
                            result.Accessibility = (BuilderAccessibility)namedArg.Value.Value!;
                            break;
                    }
                }

                return result;
            }
        }

        return null;
    }
}