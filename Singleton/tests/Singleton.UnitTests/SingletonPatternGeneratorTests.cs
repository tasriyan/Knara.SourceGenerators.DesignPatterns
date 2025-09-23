using Knara.SourceGenerators.DesignPatterns.Singleton;
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class TestSingleton
    {
        private TestSingleton() { }
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    class NonPartialClass
    {
        private NonPartialClass() { }
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN001" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReportsWarningForPublicConstructor()
    {
        var source = @"
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class PublicConstructorSingleton
    {
        public PublicConstructorSingleton() { }
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN010" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void GeneratesEagerSingleton()
    {
        var source = @"
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.Eager)]
    partial class EagerSingleton
    {
        private EagerSingleton() { }
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.LockFree)]
    partial class LockFreeSingleton
    {
        private LockFreeSingleton() { }
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
    partial class DoubleCheckedSingleton
    {
        private DoubleCheckedSingleton() { }
    }
}";

        var result = RunGenerator(source);
        var singletonSource = result.Results[0].GeneratedSources
            .First(s => s.HintName == "DoubleCheckedSingleton.Singleton.g.cs")
            .SourceText.ToString();

        Assert.Contains("private static readonly object _lock", singletonSource);
        Assert.Contains("lock (_lock)", singletonSource);
        Assert.Contains("private static volatile DoubleCheckedSingleton?", singletonSource);
    }

    [Fact]
    public void GeneratesLazySingleton()
    {
        var source = @"
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.Lazy)]
    partial class LazySingleton
    {
        private LazySingleton() { }
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class GenericSingleton<T> where T : class
    {
        private GenericSingleton() { }
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
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class SingletonWithInit
    {
        private SingletonWithInit() { }
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
    public void ReportsWarningForNonThreadSafeField()
    {
        var source = @"
using System.Collections.Generic;
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class SingletonWithNonThreadSafeField
    {
        private SingletonWithNonThreadSafeField() { }
        private Dictionary<string, int> _data = new Dictionary<string, int>();
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN007" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void HandlesMultipleSingletonsInSameFile()
    {
        var source = @"
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton]
    partial class FirstSingleton
    {
        private FirstSingleton() { }
    }

    [Singleton(Strategy = SingletonStrategy.Eager)]
    partial class SecondSingleton
    {
        private SecondSingleton() { }
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

    [Fact]
    public void ReportsGenericConstraintWarning()
    {
        var source = @"
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace TestNamespace
{
    [Singleton(Strategy = SingletonStrategy.LockFree)]
    partial class GenericSingletonWithStruct<T> where T : struct
    {
        private GenericSingletonWithStruct() { }
    }
}";

        var result = RunGenerator(source);
        var generatorResult = result.Results[0];

        Assert.Contains(generatorResult.Diagnostics,
            d => d.Id == "SIN006" && d.Severity == DiagnosticSeverity.Info);
    }
}