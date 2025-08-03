using BenchmarkDotNet.Attributes;
using CodeGenerator.Patterns.Singleton;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Singleton.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SingletonFeatureBenchmarks
{
    private const int IterationCount = 100;

    [Benchmark]
    public void AttributePropertyParsing()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var source = GenerateSourceWithAllProperties();
            var compilation = CreateCompilation(source);
            var driver = CSharpGeneratorDriver.Create(new SingletonPatternGenerator());
            _ = driver.RunGenerators(compilation).GetRunResult();
        }
    }

    [Benchmark]
    public void GenericSingletonGeneration()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var source = GenerateGenericSource();
            var compilation = CreateCompilation(source);
            var driver = CSharpGeneratorDriver.Create(new SingletonPatternGenerator());
            _ = driver.RunGenerators(compilation).GetRunResult();
        }
    }

    [Benchmark]
    public void FactoryMethodGeneration()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var source = GenerateFactorySource();
            var compilation = CreateCompilation(source);
            var driver = CSharpGeneratorDriver.Create(new SingletonPatternGenerator());
            _ = driver.RunGenerators(compilation).GetRunResult();
        }
    }

    private static string GenerateSourceWithAllProperties()
    {
        return """
            using CodeGenerator.Patterns.Singleton;

            [Singleton(Strategy = SingletonStrategy.LockFree, RegisterInDI = true, UseFactory = true)]
            public partial class TestService
            {
                public static TestService CreateInstance() => new TestService();
                private void Initialize() { }
            }
            """;
    }

    private static string GenerateGenericSource()
    {
        return """
            using CodeGenerator.Patterns.Singleton;

            [Singleton]
            public partial class TestService<T> where T : class, new()
            {
                private void Initialize() { }
            }
            """;
    }

    private static string GenerateFactorySource()
    {
        return """
            using CodeGenerator.Patterns.Singleton;

            [Singleton(UseFactory = true, FactoryMethodName = "Create")]
            public partial class TestService
            {
                public static TestService Create() => new TestService();
            }
            """;
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}