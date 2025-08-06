using CodeGenerator.Patterns.Singleton;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Singleton.UnitTests;

public class SingletonPatternGeneratorTests
{
    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var generator = new SingletonPatternGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        return driver.RunGenerators(compilation).GetRunResult();
    }

    [Fact]
    public void Initialize_AddsAttributeSource()
    {
        var source = "";
        var result = RunGenerator(source);

        Assert.Single(result.Results);
        var generatorResult = result.Results[0];
        
        Assert.Contains(generatorResult.GeneratedSources, 
            s => s.HintName == "SingletonAttribute.g.cs");
    }

    [Fact]
    public void GeneratesSingletonForValidPartialClass()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class TestSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.GeneratedSources,
            s => s.HintName == "TestSingleton.Singleton.g.cs");
        
        var singletonSource = generatorResult.GeneratedSources
            .First(s => s.HintName == "TestSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("partial class TestSingleton", singletonSource);
        Assert.Contains("public static TestSingleton Instance", singletonSource);
    }

    [Fact]
    public void ReportsErrorForNonPartialClass()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    class NonPartialClass
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN001" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void GeneratesEagerSingleton()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.Eager)]
    partial class EagerSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "EagerSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("private static readonly EagerSingleton _instance", singletonSource);
        Assert.Contains("static EagerSingleton()", singletonSource);
    }

    [Fact]
    public void GeneratesLockFreeSingleton()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.LockFree)]
    partial class LockFreeSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "LockFreeSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("private static volatile LockFreeSingleton?", singletonSource);
        Assert.Contains("Interlocked.CompareExchange", singletonSource);
    }

    [Fact]
    public void GeneratesDoubleCheckedLockingSingleton()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
    partial class DoubleCheckedSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "DoubleCheckedSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("private static readonly object _lock", singletonSource);
        Assert.Contains("lock (_lock)", singletonSource);
    }

    [Fact]
    public void GeneratesLazySingleton()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.Lazy)]
    partial class LazySingleton
    {
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "LazySingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("private static readonly Lazy<LazySingleton>", singletonSource);
        Assert.Contains("_lazy.Value", singletonSource);
    }

    [Fact]
    public void GeneratesGenericSingleton()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class GenericSingleton<T> where T : class
    {
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "GenericSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("partial class GenericSingleton<T>", singletonSource);
        Assert.Contains("where T : class", singletonSource);
        Assert.Contains("GenericSingleton<T> Instance", singletonSource);
    }

    [Fact]
    public void GeneratesSingletonWithInitializeMethod()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class SingletonWithInit
    {
        public void Initialize() { }
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "SingletonWithInit.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("instance.Initialize()", singletonSource);
    }

    [Fact]
    public void GeneratesSingletonWithFactoryMethod()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(UseFactory = true, FactoryMethodName = ""Create"")]
    partial class SingletonWithFactory
    {
        public static SingletonWithFactory Create() => new SingletonWithFactory();
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "SingletonWithFactory.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("var instance = Create()", singletonSource);
    }

    [Fact]
    public void ReportsErrorForMissingFactoryMethod()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(UseFactory = true, FactoryMethodName = ""MissingMethod"")]
    partial class SingletonMissingFactory
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN002" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReportsErrorForInvalidFactoryMethodSignature()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(UseFactory = true, FactoryMethodName = ""BadFactory"")]
    partial class SingletonBadFactory
    {
        public SingletonBadFactory BadFactory() => new SingletonBadFactory(); // Not static
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN003" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void GeneratesDIExtensionsWhenRequested()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(RegisterInDI = true)]
    partial class DIRegisteredSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.GeneratedSources,
            s => s.HintName == "DIRegisteredSingleton.DI.g.cs");

        var diSource = generatorResult.GeneratedSources
            .First(s => s.HintName == "DIRegisteredSingleton.DI.g.cs")
            .SourceText.ToString();

        Assert.Contains("AddDIRegisteredSingletonSingleton", diSource);
        Assert.Contains("services.AddSingleton", diSource);
    }

    [Fact]
    public void ReportsWarningForConflictingConfiguration()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.Eager, LazyInitialization = true)]
    partial class ConflictingSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN005" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void HandlesMultipleSingletonsInSameFile()
    {
        var source = @"
using CodeGenerator.Patterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class FirstSingleton
    {
    }

    [Singleton(Strategy = SingletonStrategy.Eager)]
    partial class SecondSingleton
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.GeneratedSources,
            s => s.HintName == "FirstSingleton.Singleton.g.cs");
        Assert.Contains(generatorResult.GeneratedSources,
            s => s.HintName == "SecondSingleton.Singleton.g.cs");
    }

    [Fact]
    public void IgnoresClassesWithoutSingletonAttribute()
    {
        var source = @"
namespace TestNamespace
{
    partial class RegularClass
    {
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        // Should only have the attribute source
        Assert.Single(generatorResult.GeneratedSources);
        Assert.Equal("SingletonAttribute.g.cs", generatorResult.GeneratedSources[0].HintName);
    }
}