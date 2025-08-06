using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CodeGenerator.Patterns.Decorator;

public class InterfaceInfo
{
    public string Name { get; }
    public string BaseName { get; }
    public string Namespace { get; }
    public List<IMethodSymbol> Methods { get; } 
    
    public InterfaceInfo(string name, string baseName, string ns, List<IMethodSymbol>? methods = null)
    {
        Name = name;
        BaseName = baseName;
        Namespace = ns;
        Methods = methods ?? [];
    }
}

public class DecoratorInfo
{
    public string ClassName { get; } 
    public string Type { get; } 
    public string TargetInterfaceName { get; } 
    public List<IParameterSymbol> ConstructorParameters { get; } 

    public DecoratorInfo(string className, string type, string interfaceName, List<IParameterSymbol> constructorParameters)
    {
        ClassName = className;
        Type = type;
        TargetInterfaceName = interfaceName;
        ConstructorParameters = constructorParameters ?? [];
    }
}

// Helper classes for diagnostic collection
public class InterfaceInfoResult
{
    public InterfaceInfo? InterfaceInfo { get; }
    public List<DiagnosticInfo> Diagnostics { get; }

    public InterfaceInfoResult(InterfaceInfo? interfaceInfo, List<DiagnosticInfo> diagnostics)
    {
        InterfaceInfo = interfaceInfo;
        Diagnostics = diagnostics ?? [];
    }
}

public class DecoratorInfoResult
{
    public DecoratorInfo? DecoratorInfo { get; }
    public List<DiagnosticInfo> Diagnostics { get; }

    public DecoratorInfoResult(DecoratorInfo? decoratorInfo, List<DiagnosticInfo> diagnostics)
    {
        DecoratorInfo = decoratorInfo;
        Diagnostics = diagnostics ?? [];
    }
}

public class DiagnosticInfo
{
    public DiagnosticDescriptor Descriptor { get; }
    public Location Location { get; }
    public object[] Args { get; }

    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location location, params object[] args)
    {
        Descriptor = descriptor;
        Location = location;
        Args = args ?? [];
    }
}

[Generator]
public class DecoratorFactoryGenerator : IIncrementalGenerator
{
    private const string GenerateDecoratorFactoryAttribute = @"
using System;

namespace CodeGenerator.Patterns.Decorator
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class GenerateDecoratorFactoryAttribute : Attribute
    {
        public string BaseName { get; set; } = """";
        public bool GenerateFactory { get; set; } = true;
    }
}";

    private const string DecoratorAttribute = @"
using System;

namespace CodeGenerator.Patterns.Decorator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DecoratorAttribute : Attribute
    {
        public string Type { get; set; } = """";
        public int Order { get; set; } = 0;
    }
}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the attribute source files
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("GenerateDecoratorFactoryAttribute.g.cs", SourceText.From(GenerateDecoratorFactoryAttribute, Encoding.UTF8));
            ctx.AddSource("DecoratorAttribute.g.cs", SourceText.From(DecoratorAttribute, Encoding.UTF8));
        });

        // Get interfaces with the attribute
        var interfaceDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsInterfaceWithAttributes(s),
                transform: static (ctx, _) => GetInterfaceDeclaration(ctx))
            .Where(static m => m is not null);

        // Get decorator classes
        var decoratorDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassWithAttributes(s),
                transform: static (ctx, _) => GetDecoratorDeclaration(ctx))
            .Where(static m => m is not null);

        // Combine and generate
        var combined = interfaceDeclarations.Combine(decoratorDeclarations.Collect());

        context.RegisterSourceOutput(combined, static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static bool IsInterfaceWithAttributes(SyntaxNode node)
    {
        return node is InterfaceDeclarationSyntax iface && iface.AttributeLists.Count > 0;
    }

    private static bool IsClassWithAttributes(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0;
    }

    private static InterfaceInfoResult? GetInterfaceDeclaration(GeneratorSyntaxContext context)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(interfaceDecl);
        var diagnostics = new List<DiagnosticInfo>();

        if (symbol == null) 
        {
            var location = interfaceDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                DecoratorDiagnostics.InterfaceSymbolNotResolved,
                location,
                interfaceDecl.Identifier.Text));
            return new InterfaceInfoResult(null, diagnostics);
        }

        var hasAttribute = symbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "GenerateDecoratorFactoryAttribute");

        if (!hasAttribute) return null;

        var attribute = symbol.GetAttributes()
            .First(attr => attr.AttributeClass?.Name == "GenerateDecoratorFactoryAttribute");

        var baseName = "";
        var generateFactory = true;

        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == "BaseName" && namedArg.Value.Value is string baseNameValue)
                baseName = baseNameValue;
            if (namedArg.Key == "GenerateFactory" && namedArg.Value.Value is bool generateFactoryValue)
                generateFactory = generateFactoryValue;
        }

        if (!generateFactory) return null;

        if (string.IsNullOrEmpty(baseName))
        {
            baseName = symbol.Name.StartsWith("I") ? symbol.Name.Substring(1) : symbol.Name;
        }

        // Check for naming conflicts with factory
        var factoryName = $"{baseName}DecoratorFactory";
        if (HasFactoryNamingConflict(symbol, factoryName))
        {
            var location = interfaceDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                DecoratorDiagnostics.FactoryNameConflict,
                location,
                baseName));
        }

        var interfaceInfo = new InterfaceInfo(
            symbol.Name,
            baseName,
            symbol.ContainingNamespace?.ToDisplayString() ?? "");

        return new InterfaceInfoResult(interfaceInfo, diagnostics);
    }

    private static DecoratorInfoResult? GetDecoratorDeclaration(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(classDecl);
        var diagnostics = new List<DiagnosticInfo>();

        if (symbol == null) return null;

        var decoratorAttr = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "DecoratorAttribute");

        if (decoratorAttr == null) return null;

        var decoratorType = "";
        foreach (var namedArg in decoratorAttr.NamedArguments)
        {
            if (namedArg.Key == "Type" && namedArg.Value.Value is string typeValue)
                decoratorType = typeValue;
        }

        // Check for missing Type property
        if (string.IsNullOrEmpty(decoratorType))
        {
            var location = classDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                DecoratorDiagnostics.MissingDecoratorType,
                location,
                symbol.Name));
            return new DecoratorInfoResult(null, diagnostics);
        }

        // Find which interface this decorator implements
        var targetInterface = symbol.AllInterfaces
            .FirstOrDefault(i => i.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "GenerateDecoratorFactoryAttribute"));

        if (targetInterface == null)
        {
            var location = classDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                DecoratorDiagnostics.DecoratorNotImplementingInterface,
                location,
                symbol.Name,
                "an interface marked with [GenerateDecoratorFactory]"));
            return new DecoratorInfoResult(null, diagnostics);
        }

        // Get constructor parameters and validate
        var constructor = symbol.Constructors.FirstOrDefault(c => !c.IsStatic);
        var parameters = constructor?.Parameters.ToList() ?? new List<IParameterSymbol>();

        // Validate constructor signature
        if (constructor == null || !parameters.Any() || 
            !SymbolEqualityComparer.Default.Equals(parameters[0].Type, targetInterface))
        {
            var location = classDecl.GetLocation();
            diagnostics.Add(new DiagnosticInfo(
                DecoratorDiagnostics.InvalidDecoratorConstructor,
                location,
                symbol.Name,
                targetInterface.Name));
        }

        var decoratorInfo = new DecoratorInfo(
            symbol.Name,
            decoratorType,
            targetInterface.Name,
            parameters);

        return new DecoratorInfoResult(decoratorInfo, diagnostics);
    }

    private static bool HasFactoryNamingConflict(INamedTypeSymbol interfaceSymbol, string factoryName)
    {
        // Check if factory name conflicts with existing types in the same namespace
        var containingNamespace = interfaceSymbol.ContainingNamespace;
        return containingNamespace.GetTypeMembers(factoryName).Any();
    }

    private static void Execute(SourceProductionContext context, InterfaceInfoResult? interfaceResult, ImmutableArray<DecoratorInfoResult?> decoratorResults)
    {
        // Report interface diagnostics
        if (interfaceResult != null)
        {
            foreach (var diagnostic in interfaceResult.Diagnostics)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    diagnostic.Descriptor,
                    diagnostic.Location,
                    diagnostic.Args));
            }
        }

        // Report decorator diagnostics
        foreach (var decoratorResult in decoratorResults.Where(d => d != null))
        {
            foreach (var diagnostic in decoratorResult!.Diagnostics)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    diagnostic.Descriptor,
                    diagnostic.Location,
                    diagnostic.Args));
            }
        }

        // Skip generation if no valid interface
        if (interfaceResult?.InterfaceInfo == null) return;

        var interfaceData = interfaceResult.InterfaceInfo;
        var validDecorators = decoratorResults
            .Where(d => d?.DecoratorInfo != null && d.DecoratorInfo.TargetInterfaceName == interfaceData.Name)
            .Select(d => d!.DecoratorInfo!)
            .ToList();

        // Check if no valid decorators found
        if (!validDecorators.Any())
        {
            var diagnostic = Diagnostic.Create(
                DecoratorDiagnostics.NoValidDecorators,
                Location.None,
                interfaceData.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        try
        {
            var source = GenerateFactory(interfaceData, validDecorators);
            context.AddSource($"{interfaceData.BaseName}DecoratorFactory.g.cs", SourceText.From(source, Encoding.UTF8));
        }
        catch (System.Exception ex)
        {
            // Report any code generation errors
            var diagnostic = Diagnostic.Create(
				DecoratorDiagnostics.CodeGenerationError,
                Location.None,
                interfaceData.Name,
                ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string GenerateFactory(InterfaceInfo interfaceData, List<DecoratorInfo> decorators)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(interfaceData.Namespace))
        {
            sb.AppendLine($"namespace {interfaceData.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine($"public static class {interfaceData.BaseName}DecoratorFactory");
        sb.AppendLine("{");

        // Generate Create method
        sb.AppendLine($"    public static {interfaceData.Name} Create({interfaceData.Name} implementation)");
        sb.AppendLine("    {");
        sb.AppendLine("        return implementation;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate extension methods for each decorator
        foreach (var decorator in decorators)
        {
            GenerateExtensionMethod(sb, interfaceData.Name, decorator);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateExtensionMethod(StringBuilder sb, string interfaceName, DecoratorInfo decorator)
    {
        var methodName = $"With{decorator.Type}";
        
        // Skip the first parameter (the interface being decorated)
        var additionalParams = decorator.ConstructorParameters.Skip(1).ToList();

        // Build parameter list
        var paramList = new StringBuilder($"this {interfaceName} service");
        foreach (var param in additionalParams)
        {
            paramList.Append($", {param.Type.ToDisplayString()} {param.Name}");
        }

        sb.AppendLine($"    public static {interfaceName} {methodName}({paramList})");
        sb.AppendLine("    {");

        // Build constructor arguments
        var args = new StringBuilder("service");
        foreach (var param in additionalParams)
        {
            args.Append($", {param.Name}");
        }

        sb.AppendLine($"        return new {decorator.ClassName}({args});");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}