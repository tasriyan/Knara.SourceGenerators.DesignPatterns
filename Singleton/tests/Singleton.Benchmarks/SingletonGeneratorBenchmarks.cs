using BenchmarkDotNet.Attributes;
using Knara.SourceGenerators.DesignPatterns.Singleton;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Singleton.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SingletonGeneratorBenchmarks
{
    private GeneratorDriver _driver = null!;
    private Compilation _simpleCompilation = null!;
    private Compilation _complexCompilation = null!;
    private Compilation _genericCompilation = null!;
    private Compilation _multipleClassesCompilation = null!;

    [GlobalSetup]
    public void Setup()
    {
        _driver = CSharpGeneratorDriver.Create(new SingletonPatternGenerator());

        _simpleCompilation = CreateCompilation(SimpleClassSource);
        _complexCompilation = CreateCompilation(ComplexClassSource);
        _genericCompilation = CreateCompilation(GenericClassSource);
        _multipleClassesCompilation = CreateCompilation(MultipleClassesSource);
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
    public GeneratorDriverRunResult GenericClass()
    {
        return _driver.RunGenerators(_genericCompilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult MultipleSingletons()
    {
        return _driver.RunGenerators(_multipleClassesCompilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult AllStrategies()
    {
        var compilation = CreateCompilation(AllStrategiesSource);
        return _driver.RunGenerators(compilation).GetRunResult();
    }

    [Benchmark]
    public GeneratorDriverRunResult WithDIRegistration()
    {
        var compilation = CreateCompilation(DIRegistrationSource);
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
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private const string SimpleClassSource = """
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton]
        public partial class SimpleLogger
        {
            private void Initialize()
            {
                // Simple initialization
            }

            public void Log(string message) { }
        }
        """;

    private const string ComplexClassSource = """
        using System.Collections.Concurrent;
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton(Strategy = SingletonStrategy.LockFree, UseFactory = true, FactoryMethodName = "CreateInstance")]
        public partial class ComplexCacheManager
        {
            private readonly ConcurrentDictionary<string, object> _cache = new();
            private readonly string _connectionString;

            public static ComplexCacheManager CreateInstance()
            {
                var connectionString = Environment.GetEnvironmentVariable("CACHE_CONNECTION") ?? "localhost:6379";
                return new ComplexCacheManager(connectionString);
            }

            private ComplexCacheManager(string connectionString)
            {
                _connectionString = connectionString;
            }

            private void Initialize()
            {
                // Complex initialization with background tasks
                _ = Task.Run(CleanupExpiredEntries);
            }

            private async Task CleanupExpiredEntries()
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    // Cleanup logic
                }
            }
        }
        """;

    private const string GenericClassSource = """
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
        public partial class GenericRepository<T, TKey> 
            where T : class, new() 
            where TKey : IComparable<TKey>
        {
            private readonly Dictionary<TKey, T> _items = new();

            private void Initialize()
            {
                // Generic repository initialization
            }

            public void Add(TKey key, T item) => _items[key] = item;
            public T? Get(TKey key) => _items.TryGetValue(key, out var item) ? item : null;
        }
        """;

    private const string MultipleClassesSource = """
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton(Strategy = SingletonStrategy.Lazy)]
        public partial class ConfigManager
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.Eager)]
        public partial class Logger
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.LockFree)]
        public partial class MetricsCollector
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
        public partial class DatabasePool
        {
            private void Initialize() { }
        }
        """;

    private const string AllStrategiesSource = """
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton(Strategy = SingletonStrategy.Lazy)]
        public partial class LazyService
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.Eager)]
        public partial class EagerService
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.LockFree)]
        public partial class LockFreeService
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
        public partial class DoubleCheckedService
        {
            private void Initialize() { }
        }
        """;

    private const string DIRegistrationSource = """
        using Knara.SourceGenerators.DesignPatterns.Singleton;

        [Singleton(RegisterInDI = true)]
        public partial class DIService
        {
            private void Initialize() { }
        }

        [Singleton(Strategy = SingletonStrategy.LockFree, RegisterInDI = true)]
        public partial class DILockFreeService
        {
            private void Initialize() { }
        }
        """;
}