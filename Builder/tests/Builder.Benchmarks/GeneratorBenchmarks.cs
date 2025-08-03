using BenchmarkDotNet.Attributes;
using CodeGenerator.Patterns.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Builder.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class GeneratorBenchmarks
{
    private GeneratorDriver _driver = null!;
    private Compilation _simpleCompilation = null!;
    private Compilation _complexCompilation = null!;
    private Compilation _largeCompilation = null!;

    [GlobalSetup]
    public void Setup()
    {
        _driver = CSharpGeneratorDriver.Create(new BuilderPatternGenerator());

        _simpleCompilation = CreateCompilation(SimpleClassSource);
        _complexCompilation = CreateCompilation(ComplexClassSource);
        _largeCompilation = CreateCompilation(LargeClassSource);
    }

    [Benchmark(Baseline = true)]
    public GeneratorDriverRunResult SimpleClass()
    {
        return _driver.RunGenerators(_simpleCompilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult ComplexClass()
    {
        return _driver.RunGenerators(_complexCompilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult LargeClass()
    {
        return _driver.RunGenerators(_largeCompilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult MultipleClasses()
    {
        var compilation = CreateCompilation(MultipleClassesSource);
        return _driver.RunGenerators(compilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult IncrementalGeneration()
    {
        // Simulate incremental generation by running twice
        var result1 = _driver.RunGenerators(_simpleCompilation);
        return result1.RunGenerators(_simpleCompilation).GetRunResult();
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private const string SimpleClassSource = """
        using CodeGenerator.Patterns.Builder;

        [GenerateBuilder]
        public class Person
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public int Age { get; set; }
        }
        """;

    private const string ComplexClassSource = """
        using System.Collections.Generic;
        using CodeGenerator.Patterns.Builder;

        [GenerateBuilder(GenerateFromMethod = true)]
        public class ComplexOrder
        {
            [BuilderProperty(Required = true)]
            public string OrderId { get; set; } = "";

            [BuilderProperty(CustomSetterName = "SetCustomerEmail")]
            public string CustomerEmail { get; set; } = "";

            public List<string> Items { get; set; } = new();
            
            public Dictionary<string, string> Metadata { get; set; } = new();

            [BuilderCollection(AddMethodName = "AddTag")]
            public List<string> Tags { get; set; } = new();

            public decimal TotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public bool IsRush { get; set; }

            [BuilderProperty(IgnoreInBuilder = true)]
            public string InternalId { get; set; } = "";
        }
        """;

    private const string LargeClassSource = """
        using System.Collections.Generic;
        using CodeGenerator.Patterns.Builder;

        [GenerateBuilder(ValidateOnBuild = true, GenerateFromMethod = true)]
        public class LargeConfiguration
        {
            [BuilderProperty(Required = true)]
            public string ApplicationName { get; set; } = "";

            [BuilderProperty(Required = true)]
            public string Version { get; set; } = "";

            public string Environment { get; set; } = "";
            public string Region { get; set; } = "";
            public string DataCenter { get; set; } = "";
            public string ServiceUrl { get; set; } = "";
            public string DatabaseConnectionString { get; set; } = "";
            public string RedisConnectionString { get; set; } = "";
            public string MessageQueueUrl { get; set; } = "";
            public string LoggingLevel { get; set; } = "";
            public string MetricsEndpoint { get; set; } = "";
            
            public int MaxRetries { get; set; }
            public int TimeoutSeconds { get; set; }
            public int MaxConcurrency { get; set; }
            public int BufferSize { get; set; }
            public int CacheTimeoutMinutes { get; set; }
            
            public bool EnableMetrics { get; set; }
            public bool EnableTracing { get; set; }
            public bool EnableHealthChecks { get; set; }
            public bool EnableCircuitBreaker { get; set; }
            public bool EnableRetry { get; set; }
            
            public decimal RequestRateLimit { get; set; }
            public decimal ErrorThreshold { get; set; }
            
            public DateTime StartTime { get; set; }
            public TimeSpan GracefulShutdownTimeout { get; set; }
            
            public List<string> AllowedOrigins { get; set; } = new();
            public List<string> EnabledFeatures { get; set; } = new();
            public List<string> SecurityHeaders { get; set; } = new();
            
            public Dictionary<string, string> AppSettings { get; set; } = new();
            public Dictionary<string, string> ConnectionStrings { get; set; } = new();
            public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        }
        """;

    private const string MultipleClassesSource = """
        using System.Collections.Generic;
        using CodeGenerator.Patterns.Builder;

        [GenerateBuilder]
        public class User
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
        }

        [GenerateBuilder]
        public class Order
        {
            public string OrderId { get; set; } = "";
            public List<string> Items { get; set; } = new();
        }

        [GenerateBuilder]
        public class Product
        {
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
        }

        [GenerateBuilder]
        public class Category
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
        }

        [GenerateBuilder]
        public class Address
        {
            public string Street { get; set; } = "";
            public string City { get; set; } = "";
            public string PostalCode { get; set; } = "";
        }
        """;
}