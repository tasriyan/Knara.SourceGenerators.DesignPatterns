using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Knara.SourceGenerators.DesignPatterns.Singleton;

[Generator]
public class SingletonPatternGenerator : IIncrementalGenerator
{
    private const string SingletonAttribute = @"
using System;

namespace Knara.SourceGenerators.DesignPatterns.Singleton
{
    public enum SingletonStrategy
    {
        /// <summary>Uses Lazy&lt;T&gt; - Good performance, simple implementation</summary>
        Lazy = 0,
        /// <summary>Eager initialization - Fastest access, initialized at startup</summary>
        Eager = 1,
        /// <summary>Lock-free with Interlocked - Very fast, lazy initialization</summary>
        LockFree = 2,
        /// <summary>Double-checked locking - Classic pattern, balanced performance</summary>
        DoubleCheckedLocking = 3
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonAttribute : Attribute
    {
        /// <summary>Strategy for singleton implementation</summary>
        public SingletonStrategy Strategy { get; set; } = SingletonStrategy.LockFree;
      
    }
}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the attribute source file
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("SingletonAttribute.g.cs", SourceText.From(SingletonAttribute, Encoding.UTF8));
        });

        // Get classes with the Singleton attribute
        var singletonClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsClassWithSingletonAttribute(s),
                transform: static (ctx, _) => GetSingletonClassInfo(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(singletonClasses, static (spc, source) => GenerateSingletonImplementation(spc, source!));
    }

    private static bool IsClassWithSingletonAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0;
    }

private static SingletonClassInfoResult? GetSingletonClassInfo(GeneratorSyntaxContext context)
{
    var classDecl = (ClassDeclarationSyntax)context.Node;
    var model = context.SemanticModel;
    var symbol = model.GetDeclaredSymbol(classDecl);
    var diagnostics = new List<DiagnosticInfo>();

    if (symbol == null) 
    {
        var location = classDecl.GetLocation();
        diagnostics.Add(new DiagnosticInfo(
            SingletonDiagnostics.TypeSymbolNotResolved,
            location,
            classDecl.Identifier.Text));
        return new SingletonClassInfoResult(null, diagnostics);
    }

    var singletonAttr = symbol.GetAttributes()
        .FirstOrDefault(attr => attr.AttributeClass?.Name == "SingletonAttribute");

    if (singletonAttr == null) return null;

    // Check if class is partial
    if (!classDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
    {
        var location = classDecl.GetLocation();
        diagnostics.Add(new DiagnosticInfo(
            SingletonDiagnostics.ClassNotPartial,
            location,
            symbol.Name));
        return new SingletonClassInfoResult(null, diagnostics);
    }

    // Parse attribute properties (existing code...)
    var strategy = SingletonStrategy.LockFree;
    var factoryMethodName = "CreateInstance";

    foreach (var namedArg in singletonAttr.NamedArguments)
    {
        switch (namedArg.Key)
        {
            case "Strategy":
                if (namedArg.Value.Value is int strategyValue)
                    strategy = (SingletonStrategy)strategyValue;
                break;
            case "FactoryMethodName":
                if (namedArg.Value.Value is string methodName)
                    factoryMethodName = methodName;
                break;
        }
    }
    
    // Validate thread-safety
    ValidateThreadSafety(symbol, diagnostics, classDecl.GetLocation());
    
    // Add this line after ValidateThreadSafety call
    ValidateConstructorAccess(symbol, diagnostics, classDecl.GetLocation());

    // Check if class has Initialize method (existing code...)
    var hasInitializeMethod = symbol.GetMembers("Initialize")
        .OfType<IMethodSymbol>()
        .Any(m => m.Parameters.IsEmpty && m.ReturnsVoid);
    
    // Rest of the method remains the same...
    var isGeneric = symbol.TypeParameters.Length > 0;
    var typeParameters = symbol.TypeParameters.Select(tp => tp.Name).ToList();
    var typeConstraints = symbol.TypeParameters
        .Select(tp => GetTypeConstraints(tp))
        .Where(c => !string.IsNullOrEmpty(c))
        .ToList();

    if (isGeneric)
    {
        ValidateGenericConstraints(symbol, strategy, diagnostics, classDecl.GetLocation());
    }

    var singletonInfo = new SingletonClassInfo(
        className: symbol.Name,
        ns: symbol.ContainingNamespace?.ToDisplayString() ?? "",
        strategy: strategy,
        hasInitializeMethod: hasInitializeMethod,
        isGeneric: isGeneric,
        typeParameters: typeParameters,
        typeConstraints: typeConstraints);

    return new SingletonClassInfoResult(singletonInfo, diagnostics);
}

    private static void ValidateGenericConstraints(INamedTypeSymbol symbol, SingletonStrategy strategy, List<DiagnosticInfo> diagnostics, Location location)
    {
        foreach (var typeParam in symbol.TypeParameters)
        {
            // Warn about value type constraints with certain strategies
            if (typeParam.HasValueTypeConstraint && strategy == SingletonStrategy.LockFree)
            {
                diagnostics.Add(new DiagnosticInfo(
                    SingletonDiagnostics.GenericConstraintWarning,
                    location,
                    symbol.Name,
                    strategy.ToString(),
                    "struct constraint"));
            }

            // Warn about constructor constraints with factory methods
            if (typeParam.HasConstructorConstraint && symbol.GetMembers().OfType<IMethodSymbol>().Any(m => m.IsStatic && m.Name.Contains("Create")))
            {
                diagnostics.Add(new DiagnosticInfo(
                    SingletonDiagnostics.GenericConstraintWarning,
                    location,
                    symbol.Name,
                    strategy.ToString(),
                    "new() constraint with factory method"));
            }
        }
    }

    private static void ValidateThreadSafety(INamedTypeSymbol symbol, List<DiagnosticInfo> diagnostics, Location location)
{
    // Known non-thread-safe collection types
    var nonThreadSafeTypes = new HashSet<string>
    {
        "System.Collections.Generic.Dictionary",
        "System.Collections.Generic.List",
        "System.Collections.Generic.HashSet",
        "System.Collections.Generic.SortedDictionary",
        "System.Collections.Generic.SortedList",
        "System.Collections.Generic.SortedSet",
        "System.Collections.Generic.Queue",
        "System.Collections.Generic.Stack",
        "System.Collections.ArrayList",
        "System.Collections.Hashtable",
        "System.Collections.Queue",
        "System.Collections.Stack",
        "System.Text.StringBuilder" // StringBuilder is not thread-safe
    };

    // Thread-safe alternatives mapping
    var threadSafeAlternatives = new Dictionary<string, string>
    {
        ["System.Collections.Generic.Dictionary"] = "ConcurrentDictionary",
        ["System.Collections.Generic.List"] = "ConcurrentBag or use locks",
        ["System.Collections.Generic.HashSet"] = "ConcurrentDictionary<T, byte> or use locks",
        ["System.Collections.Generic.Queue"] = "ConcurrentQueue",
        ["System.Collections.Generic.Stack"] = "ConcurrentStack",
        ["System.Text.StringBuilder"] = "use locks or thread-local storage"
    };

    // Check fields
    foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
    {
        if (field.IsStatic) continue; // Static fields are handled differently
        
        var fieldTypeName = GetFullTypeName(field.Type);
        if (IsNonThreadSafeType(fieldTypeName, nonThreadSafeTypes))
        {
            var alternative = threadSafeAlternatives.TryGetValue(fieldTypeName, out var alt) ? $" Consider using {alt}." : "";
            
            diagnostics.Add(new DiagnosticInfo(
                SingletonDiagnostics.NonThreadSafeField,
                location,
                field.Name,
                fieldTypeName,
                symbol.Name + alternative));
        }
    }

    // Check properties
    foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>())
    {
        if (property.IsStatic) continue;
        
        var propertyTypeName = GetFullTypeName(property.Type);
        if (IsNonThreadSafeType(propertyTypeName, nonThreadSafeTypes))
        {
            var alternative = threadSafeAlternatives.TryGetValue(propertyTypeName, out var alt) ? $" Consider using {alt}." : "";
            
            diagnostics.Add(new DiagnosticInfo(
                SingletonDiagnostics.NonThreadSafeProperty,
                location,
                property.Name,
                propertyTypeName,
                symbol.Name + alternative));
        }
    }

    // Check methods for non-thread-safe operations
    foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>())
    {
        if (method.IsStatic || method.MethodKind != MethodKind.Ordinary) continue;
        
        // This is a simplified check - in a real implementation, you'd analyze the method body
        // For now, we'll check method names that suggest non-thread-safe operations
        var methodName = method.Name.ToLower();
        if (methodName.Contains("set") || methodName.Contains("add") || methodName.Contains("remove") || 
            methodName.Contains("update") || methodName.Contains("modify") || methodName.Contains("clear"))
        {
            // Check if method parameters or return types involve non-thread-safe collections
            foreach (var param in method.Parameters)
            {
                var paramTypeName = GetFullTypeName(param.Type);
                if (IsNonThreadSafeType(paramTypeName, nonThreadSafeTypes))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        SingletonDiagnostics.NonThreadSafeMethodAccess,
                        location,
                        method.Name,
                        symbol.Name,
                        $"parameter '{param.Name}' of type {paramTypeName}"));
                }
            }
        }
    }
}
    
    private static void ValidateConstructorAccess(INamedTypeSymbol symbol, List<DiagnosticInfo> diagnostics, Location location)
    {
        // Check for public constructors
        var publicConstructors = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && 
                        m.DeclaredAccessibility == Accessibility.Public &&
                        !m.IsStatic)
            .ToList();

        if (publicConstructors.Any())
        {
            diagnostics.Add(new DiagnosticInfo(
                SingletonDiagnostics.PublicConstructorWarning,
                location,
                symbol.Name));
        }
    }
    
    private static string GetTypeConstraints(ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<string>();

        if (typeParameter.HasReferenceTypeConstraint)
            constraints.Add("class");
        if (typeParameter.HasValueTypeConstraint)
            constraints.Add("struct");
        if (typeParameter.HasUnmanagedTypeConstraint)
            constraints.Add("unmanaged");
        if (typeParameter.HasNotNullConstraint)
            constraints.Add("notnull");

        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            constraints.Add(constraintType.ToDisplayString());
        }

        if (typeParameter.HasConstructorConstraint)
            constraints.Add("new()");

        return constraints.Any() ? $"where {typeParameter.Name} : {string.Join(", ", constraints)}" : "";
    }

    private static void GenerateSingletonImplementation(SourceProductionContext context, SingletonClassInfoResult result)
    {
        // Report all diagnostics first
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                diagnostic.Descriptor,
                diagnostic.Location,
                diagnostic.Args));
        }

        // Skip generation if no valid singleton info
        if (result.SingletonClassInfo == null) return;

        try
        {
            var source = GenerateSingletonClass(result.SingletonClassInfo);
            context.AddSource($"{result.SingletonClassInfo.ClassName}.Singleton.g.cs", SourceText.From(source, Encoding.UTF8));
        }
        catch (System.Exception ex)
        {
            // Report any code generation errors
            context.ReportDiagnostic(Diagnostic.Create(
				SingletonDiagnostics.CodeGenerationError,
                Location.None,
                result.SingletonClassInfo.ClassName,
                ex.Message));
        }
    }

    private static string GenerateSingletonClass(SingletonClassInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        // Generate class declaration with generics
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";
        sb.AppendLine($"partial class {info.ClassName}{genericDeclaration}");

        // Add type constraints
        foreach (var constraint in info.TypeConstraints)
        {
            sb.AppendLine($"    {constraint}");
        }

        sb.AppendLine("{");

        // Generate singleton implementation based on strategy
        switch (info.Strategy)
        {
            case SingletonStrategy.Eager:
                GenerateEagerSingleton(sb, info);
                break;
            case SingletonStrategy.LockFree:
                GenerateLockFreeSingleton(sb, info);
                break;
            case SingletonStrategy.DoubleCheckedLocking:
                GenerateDoubleCheckedLockingSingleton(sb, info);
                break;
            case SingletonStrategy.Lazy:
            default:
                GenerateLazySingleton(sb, info);
                break;
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateEagerSingleton(StringBuilder sb, SingletonClassInfo info)
    {
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";

        // True eager initialization - instance created at class load time
        sb.AppendLine($"    private static readonly {className}{genericDeclaration} _instance = CreateSingletonInstance();");
        sb.AppendLine();
        sb.AppendLine($"    static {className}() {{ }} // Explicit static constructor to ensure beforefieldinit behavior");
        sb.AppendLine();
        sb.AppendLine($"    public static {className}{genericDeclaration} Instance => _instance;");
        sb.AppendLine();

        GenerateCreateInstanceMethod(sb, info);
    }

    private static void GenerateLockFreeSingleton(StringBuilder sb, SingletonClassInfo info)
    {
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";

        sb.AppendLine($"    private static volatile {className}{genericDeclaration}? _instance;");
        sb.AppendLine("    private static int _isInitialized = 0;");
        sb.AppendLine();
        
        sb.AppendLine($"    public static {className}{genericDeclaration} Instance");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_instance != null) return _instance; // Fast path");
        sb.AppendLine("            return GetOrCreateInstance();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        sb.AppendLine($"    private static {className}{genericDeclaration} GetOrCreateInstance()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // We won the race - create the instance");
        sb.AppendLine("            var newInstance = CreateSingletonInstance();");
        sb.AppendLine("            Interlocked.Exchange(ref _instance, newInstance); // Atomic assignment with memory barrier");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Another thread is creating the instance - spin wait");
        sb.AppendLine("            SpinWait.SpinUntil(() => _instance != null);");
        sb.AppendLine("        }");
        sb.AppendLine("        return _instance!;");
        sb.AppendLine("    }");
        sb.AppendLine();
        
        GenerateCreateInstanceMethod(sb, info);
    }

    private static void GenerateDoubleCheckedLockingSingleton(StringBuilder sb, SingletonClassInfo info)
    {
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";

        sb.AppendLine($"    private static volatile {className}{genericDeclaration}? _instance;");
        sb.AppendLine("    private static readonly object _lock = new object();");
        sb.AppendLine();

        sb.AppendLine($"    public static {className}{genericDeclaration} Instance");
        sb.AppendLine("    {");
        sb.AppendLine("        get");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_instance == null)"); 
        sb.AppendLine("            {");
        sb.AppendLine("                lock (_lock)");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (_instance == null)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        _instance = CreateSingletonInstance();");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            return _instance;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        GenerateCreateInstanceMethod(sb, info);
    }

    private static void GenerateLazySingleton(StringBuilder sb, SingletonClassInfo info)
    {
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";

        sb.AppendLine($"    private static readonly Lazy<{className}{genericDeclaration}> _lazy =");
        sb.AppendLine($"        new Lazy<{className}{genericDeclaration}>(CreateSingletonInstance);");
        sb.AppendLine();
        sb.AppendLine($"    public static {className}{genericDeclaration} Instance => _lazy.Value;");
        sb.AppendLine();

        GenerateCreateInstanceMethod(sb, info);
    }

    private static void GenerateCreateInstanceMethod(StringBuilder sb, SingletonClassInfo info)
    {
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";

        sb.AppendLine($"    private static {className}{genericDeclaration} CreateSingletonInstance()");
        sb.AppendLine("    {");
        
        sb.AppendLine($"        var instance = new {className}{genericDeclaration}();");
        
        if (info.HasInitializeMethod)
        {
            sb.AppendLine("        instance.Initialize();");
        }
        
        sb.AppendLine("        return instance;");
        sb.AppendLine("    }");
    }
    
    private static string GetFullTypeName(ITypeSymbol type)
    {
        // Handle generic types
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var baseTypeName = namedType.ConstructedFrom.ToDisplayString();
            return baseTypeName;
        }
    
        return type.ToDisplayString();
    }

    private static bool IsNonThreadSafeType(string typeName, HashSet<string> nonThreadSafeTypes)
    {
        // Direct match
        if (nonThreadSafeTypes.Contains(typeName)) return true;
    
        // Check for generic variants (e.g., Dictionary<TKey, TValue>)
        foreach (var nonThreadSafeType in nonThreadSafeTypes)
        {
            if (typeName.StartsWith(nonThreadSafeType + "<") || typeName.StartsWith(nonThreadSafeType + "`"))
            {
                return true;
            }
        }
    
        return false;
    }
}