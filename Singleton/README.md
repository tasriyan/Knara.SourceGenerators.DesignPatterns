# Singleton Pattern Source Generator

A C# source generator that automatically implements the Singleton design pattern with multiple strategies, thread-safety options, and advanced features like generic support and dependency injection integration.

## Features

- **Multiple Implementation Strategies**: Choose from 4 different singleton patterns optimized for different scenarios
- **Thread-Safe by Default**: All implementations are thread-safe with configurable options
- **Generic Support**: Full support for generic singleton classes with type constraints
- **Factory Method Integration**: Support for complex initialization scenarios
- **Dependency Injection Ready**: Automatic DI container registration
- **High Performance**: Optimized implementations with minimal overhead
- **Incremental Generation**: Fast compilation times with incremental source generation

## Installation
```bash
<PackageReference Include="SourceGenerators.DesignPatterns.Singleton" Version="1.0.0" />
```
## Quick Start

Simply add the `[Singleton]` attribute to any partial class:[Singleton] 
```csharp
public partial class ConfigurationManager
{ 
    private Dictionary<string, string> _settings;
    private void Initialize()
    {
        _settings = new Dictionary<string, string>
        {
            ["Environment"] = "Production",
            ["Version"] = "1.0.0"
        };
    }

    public string GetSetting(string key) => _settings.TryGetValue(key, out var value) ? value : "";
}

// Usage 
var config = ConfigurationManager.Instance;
```

## Singleton Strategies

### 1. Lock-Free (Default)
**Best for**: High-throughput, read-heavy scenarios

#### Pros:
- ✅ **Excellent Performance**: Uses `Interlocked.CompareExchange` for near lock-free operation
- ✅ **Low Contention**: Minimal thread blocking with optimistic concurrency
- ✅ **Fast Path Optimization**: Once initialized, access is just a null check
- ✅ **Memory Efficient**: Uses volatile fields and memory barriers effectively
- ✅ **Scalable**: Performance doesn't degrade significantly under high concurrency

#### Cons:
- ❌ **Complexity**: More complex implementation than other strategies
- ❌ **Spin-Wait**: Threads may briefly spin-wait during initialization
```csharp
[Singleton(Strategy = SingletonStrategy.LockFree)] 
public partial class MetricsCollector 
{ 
    // Ultra-fast access with Interlocked operations 
}
```

### 2. Lazy<T>
**Best for**: Simple scenarios with good performance

#### Pros:
- ✅ **Simple**: Leverages .NET's proven `Lazy<T>` implementation
- ✅ **Thread-Safe**: Built-in thread safety with no custom synchronization code
- ✅ **Lazy Loading**: Defers initialization until first access
- ✅ **Exception Safe**: Handles initialization exceptions gracefully
- ✅ **Reliable**: Well-tested framework implementation

#### Cons:
- ❌ **Moderate Performance**: Slightly slower than lock-free due to internal locking
- ❌ **Memory Overhead**: Additional `Lazy<T>` wrapper object
- ❌ **Less Control**: Can't customize the synchronization behavior
```csharp
[Singleton(Strategy = SingletonStrategy.Lazy)] 
public partial class SimpleService 
{ 
    // Uses .NET's Lazy<T> for thread-safe initialization 
}
```

### 3. Double-Checked Locking
**Best for**: Classic pattern, balanced performance, works well with generics

#### Pros:
- ✅ **Proven Pattern**: Well-established, widely understood implementation
- ✅ **Good Performance**: Fast path after initialization with minimal overhead
- ✅ **Generic-Friendly**: Works excellently with generic classes
- ✅ **Predictable**: Clear synchronization semantics with locks
- ✅ **Low Memory**: Minimal memory overhead

#### Cons:
- ❌ **Lock Overhead**: Uses locks during initialization phase
- ❌ **Potential Contention**: Multiple threads may block on the lock
- ❌ **Volatile Requirement**: Requires volatile field for correctness
- ❌ **Platform Sensitivity**: Memory model considerations on some platforms
```csharp
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)] 
public partial class Repository<T> where T : class, new() 
{ 
    // Traditional double-checked locking pattern 
}
```
### 4. Eager Initialization
**Best for**: Ultra-fast access, startup initialization acceptable

#### Pros:
- ✅ **Fastest Access**: Zero synchronization overhead after type initialization
- ✅ **Thread-Safe**: CLR guarantees thread-safe static initialization
- ✅ **Simple**: Straightforward implementation with no complex synchronization
- ✅ **Predictable Timing**: Initialization happens at predictable time (type load)
- ✅ **No Lazy Loading Issues**: Instance is always ready

#### Cons:
- ❌ **Startup Cost**: Initialization happens whether instance is used or not
- ❌ **Memory Usage**: Instance created immediately, consuming memory early
- ❌ **Initialization Order**: Can cause issues with complex dependency chains
- ❌ **Exception Handling**: Initialization exceptions can be harder to handle
- ❌ **No Lazy Benefits**: Can't defer expensive initialization
```csharp
[Singleton(Strategy = SingletonStrategy.Eager)] 
public partial class Logger 
{ 
    // Pre-initialized at application startup 
}
```

## Strategy Comparison Matrix

| Strategy | Access Speed | Initialization | Memory Usage | Complexity | Best Use Case |
|----------|-------------|----------------|--------------|------------|---------------|
| **Lock-Free** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | High-throughput services |
| **Lazy<T>** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | General purpose, simple cases |
| **Double-Checked** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | Generic classes, balanced needs |
| **Eager** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ | Critical path, fast access required |


## Advanced Features

### Generic Singletons
Full support for generic classes with type constraints:
```csharp
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)] 
public partial class Repository<T> where T : class, new() 
{ 
    private readonly List<T> _items = [];
    private void Initialize()
    {
        Console.WriteLine($"Repository<{typeof(T).Name}> initialized");
    }

    public void Add(T item) => _items.Add(item);
    public IReadOnlyList<T> GetAll() => _items.AsReadOnly();
}
// Usage 
var userRepo = Repository<User>.Instance; 
var productRepo = Repository<Product>.Instance;
```

### Factory Method Support
For complex initialization scenarios:
```csharp
[Singleton(Strategy = SingletonStrategy.LockFree, UseFactory = true)] 
public partial class DatabaseConnectionPool 
{ 
    private readonly string _connectionString; 
    private readonly int _maxPoolSize;
    public static DatabaseConnectionPool CreateInstance()
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                            ?? "Server=localhost;Database=MyApp;";
        var maxPoolSize = int.Parse(Environment.GetEnvironmentVariable("POOL_SIZE") ?? "10");
    
        return new DatabaseConnectionPool(connectionString, maxPoolSize);
    }

    private DatabaseConnectionPool(string connectionString, int maxPoolSize)
    {
        _connectionString = connectionString;
        _maxPoolSize = maxPoolSize;
        // Complex initialization logic...
    }
}
```
### Dependency Injection Integration
Automatic registration with DI containers:
```csharp
[Singleton(Strategy = SingletonStrategy.LockFree, RegisterInDI = true)] 
public partial class CacheManager 
{ 
    // Automatically generates extension methods for IServiceCollection 
}
// In Startup.cs or Program.cs 
services.AddCacheManagerSingleton();
```
## Configuration Options
```csharp
[Singleton( 
    Strategy = SingletonStrategy.LockFree,  // Implementation strategy 
    LazyInitialization = true,              // Defer initialization (ignored for Eager) 
    ThreadSafe = true,                      // Thread-safety (default: true) 
    RegisterInDI = false,                   // Generate DI extensions (default: false) 
    UseFactory = false,                     // Use factory method (default: false) 
    FactoryMethodName = "CreateInstance"    // Factory method name (default: "CreateInstance") 
    )] 
public partial class MyService { }
```
## Performance Benchmarks

Based on comprehensive benchmarks running on .NET 9.0.7 (Intel Core i7-10750H):

### Runtime Performance
| Scenario | Mean Time | Memory Allocated | Strategy Recommendation |
|----------|-----------|------------------|------------------------|
| Simple Class | 300.4 μs | 87.44 KB | Lock-Free (default) |
| Complex Class | 368.2 μs | 95.24 KB | Lock-Free |
| Generic Class | 389.0 μs | 98.70 KB | Double-Checked Locking |
| Multiple Singletons | 459.2 μs | 126.61 KB | Lock-Free |
| All Strategies | 18.4 ms | 638.7 KB | Strategy-specific optimization |
| With DI Registration | 18.2 ms | 622.0 KB | Lock-Free with DI |
| Incremental Generation | 481.0 μs | 148.94 KB | Fast incremental builds |

### Code Generation Performance
| Feature | Mean Time | Memory Usage | Description |
|---------|-----------|--------------|-------------|
| Attribute Property Parsing | 774.4 ms | 56.02 MB | Parsing singleton attributes and properties |
| Generic Singleton Generation | 751.2 ms | 55.33 MB | Generating code for generic singletons |
| Factory Method Generation | 790.4 ms | 55.30 MB | Generating factory method implementations |

### Performance Characteristics

- **Lock-Free**: Fastest runtime performance across most scenarios (300-459 μs)
- **Low Memory Overhead**: Efficient memory usage (87-149 KB for typical scenarios)
- **Fast Code Generation**: Sub-second generation times for most features
- **Scalable**: Performance remains consistent across different complexity levels
- **Generic-Optimized**: Specialized handling for generic singleton classes

### Memory Efficiency
- Simple singletons: ~87 KB allocated
- Complex singletons: ~95 KB allocated  
- Generic singletons: ~99 KB allocated
- Multiple singletons: ~127 KB allocated
- High-concurrency scenarios: ~639 KB allocated

## Requirements

- .NET Standard 2.0 or higher
- C# 8.0 or higher (for source generators)
- Partial class declaration

## Generated Code Example

For a basic singleton, the generator creates:
```csharp
// <auto-generated />
#nullable enable
using System;
using System.Threading;

namespace Demo.Singleton.ConsoleApp;

partial class ConfigurationManager
{
    private static volatile ConfigurationManager? _instance;
    private static int _isInitialized = 0;

    public static ConfigurationManager Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            return GetOrCreateInstance();
        }
    }

    private static ConfigurationManager GetOrCreateInstance()
    {
        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
        {
            // We won the race - create the instance
            var newInstance = CreateSingletonInstance();
            Interlocked.Exchange(ref _instance, newInstance); // Atomic assignment with memory barrier
        }
        else
        {
            // Another thread is creating the instance - spin wait
            SpinWait.SpinUntil(() => _instance != null);
        }
        return _instance!;
    }

    private static ConfigurationManager CreateSingletonInstance()
    {
        var instance = new ConfigurationManager();
        instance.Initialize();
        return instance;
    }
}
```
## Best Practices

1. **Choose the Right Strategy**:
   - Use `LockFree` for most scenarios
   - Use `Eager` when fastest access is critical
   - Use `DoubleCheckedLocking` for generic singletons
   - Use `Lazy` for simple, low-contention scenarios

2. **Initialization**:
   - Implement an `Initialize()` method for setup logic
   - Keep initialization lightweight for better performance
   - Use factory methods for complex initialization

3. **Thread Safety**:
   - All generated code is thread-safe by default
   - Your instance methods should also be thread-safe
   - Use concurrent collections when needed

4. **Generic Constraints**:
   - Apply appropriate type constraints
   - Consider the impact on compilation time

## Troubleshooting

- Ensure your class is declared as `partial`
- The `[Singleton]` attribute must be applied to the class
- For factory methods, ensure the method is `static` and parameterless
- Generic singletons require explicit type parameters when accessing

## License

Apache License 2.0