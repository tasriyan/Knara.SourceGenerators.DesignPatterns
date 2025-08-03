using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CodeGenerator.Patterns.Singleton;

[Generator]
public class SingletonPatternGenerator : IIncrementalGenerator
{
    private const string SingletonAttribute = @"
using System;

namespace CodeGenerator.Patterns.Singleton
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
        
        /// <summary>Whether to use lazy initialization (ignored for Eager strategy)</summary>
        public bool LazyInitialization { get; set; } = true;
        
        /// <summary>Whether the singleton should be thread-safe</summary>
        public bool ThreadSafe { get; set; } = true;
        
        /// <summary>Whether to register the singleton in DI container</summary>
        public bool RegisterInDI { get; set; } = false;
        
        /// <summary>Whether to use a factory method instead of constructor</summary>
        public bool UseFactory { get; set; } = false;
        
        /// <summary>Name of the factory method (default: CreateInstance)</summary>
        public string FactoryMethodName { get; set; } = ""CreateInstance"";
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
                predicate: static (s, _) => IsPartialClassWithSingletonAttribute(s),
                transform: static (ctx, _) => GetSingletonClassInfo(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(singletonClasses, static (spc, source) => GenerateSingletonImplementation(spc, source));
    }

    private static bool IsPartialClassWithSingletonAttribute(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax cls && 
               cls.AttributeLists.Count > 0 && 
               cls.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static SingletonClassInfo? GetSingletonClassInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var symbol = model.GetDeclaredSymbol(classDecl);

        if (symbol == null) return null;

        var singletonAttr = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "SingletonAttribute");

        if (singletonAttr == null) return null;

        // Parse attribute properties
        var strategy = SingletonStrategy.LockFree;
        var lazyInitialization = true;
        var threadSafe = true;
        var registerInDI = false;
        var useFactory = false;
        var factoryMethodName = "CreateInstance";

        foreach (var namedArg in singletonAttr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Strategy":
                    if (namedArg.Value.Value is int strategyValue)
                        strategy = (SingletonStrategy)strategyValue;
                    break;
                case "LazyInitialization":
                    if (namedArg.Value.Value is bool lazyValue)
                        lazyInitialization = lazyValue;
                    break;
                case "ThreadSafe":
                    if (namedArg.Value.Value is bool threadSafeValue)
                        threadSafe = threadSafeValue;
                    break;
                case "RegisterInDI":
                    if (namedArg.Value.Value is bool registerValue)
                        registerInDI = registerValue;
                    break;
                case "UseFactory":
                    if (namedArg.Value.Value is bool factoryValue)
                        useFactory = factoryValue;
                    break;
                case "FactoryMethodName":
                    if (namedArg.Value.Value is string methodName)
                        factoryMethodName = methodName;
                    break;
            }
        }

        // Check if class has Initialize method
        var hasInitializeMethod = symbol.GetMembers("Initialize")
            .OfType<IMethodSymbol>()
            .Any(m => m.Parameters.IsEmpty && m.ReturnsVoid);

        // Check if class has factory method
        var hasFactoryMethod = symbol.GetMembers(factoryMethodName)
            .OfType<IMethodSymbol>()
            .Any(m => m.IsStatic && m.Parameters.IsEmpty);

        // Check if class is generic
        var isGeneric = symbol.TypeParameters.Length > 0;
        var typeParameters = symbol.TypeParameters.Select(tp => tp.Name).ToList();
        var typeConstraints = symbol.TypeParameters
            .Select(tp => GetTypeConstraints(tp))
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        return new SingletonClassInfo(
            symbol.Name,
            symbol.ContainingNamespace?.ToDisplayString() ?? "",
            strategy,
            lazyInitialization,
            threadSafe,
            registerInDI,
            useFactory,
            factoryMethodName,
            hasInitializeMethod,
            hasFactoryMethod,
            isGeneric,
            typeParameters,
            typeConstraints);
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

    private static void GenerateSingletonImplementation(SourceProductionContext context, SingletonClassInfo? singletonInfo)
    {
        if (singletonInfo == null) return;

        var source = GenerateSingletonClass(singletonInfo);
        context.AddSource($"{singletonInfo.ClassName}.Singleton.g.cs", SourceText.From(source, Encoding.UTF8));

        // Generate DI extension methods if requested
        if (singletonInfo.RegisterInDI)
        {
            var diSource = GenerateDIExtensions(singletonInfo);
            context.AddSource($"{singletonInfo.ClassName}.DI.g.cs", SourceText.From(diSource, Encoding.UTF8));
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
        
        sb.AppendLine($"    private static readonly {className}{genericDeclaration} _instance = CreateSingletonInstance();");
        sb.AppendLine();
        sb.AppendLine($"    static {className}() {{ }} // Explicit static constructor for beforefieldinit");
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
        sb.AppendLine("            if (_instance != null) return _instance; // Fast path");
        sb.AppendLine();
        sb.AppendLine("            lock (_lock)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (_instance == null) // Double check inside lock");
        sb.AppendLine("                {");
        sb.AppendLine("                    _instance = CreateSingletonInstance();");
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
        
        if (info.UseFactory && info.HasFactoryMethod)
        {
            sb.AppendLine($"        var instance = {info.FactoryMethodName}();");
        }
        else
        {
            sb.AppendLine($"        var instance = new {className}{genericDeclaration}();");
        }
        
        if (info.HasInitializeMethod)
        {
            sb.AppendLine("        instance.Initialize();");
        }
        
        sb.AppendLine("        return instance;");
        sb.AppendLine("    }");
    }

    private static string GenerateDIExtensions(SingletonClassInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        sb.AppendLine("public static class SingletonDIExtensions");
        sb.AppendLine("{");
        
        var className = info.ClassName;
        var genericDeclaration = info.IsGeneric ? $"<{string.Join(", ", info.TypeParameters)}>" : "";
        var methodName = $"Add{className}Singleton";
        
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Registers {className} as a singleton in the DI container");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static IServiceCollection {methodName}(this IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine($"        services.AddSingleton(provider => {className}{genericDeclaration}.Instance);");
        sb.AppendLine("        return services;");
        sb.AppendLine("    }");
        
        sb.AppendLine("}");

        return sb.ToString();
    }
    
}