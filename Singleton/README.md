# Singleton Pattern Generator

A C# source generator that creates thread-safe singleton implementations for .NET Framework applications. Automatically generates correct singleton code and validates thread safety to prevent common concurrency bugs.

## Why This Generator Exists

**Problem**: Manual singleton implementations are error-prone and often contain race conditions that cause intermittent bugs in production. Without dependency injection containers (available in .NET Framework 4.x), singletons are essential for managing shared state.

**Solution**: Generate proven, thread-safe singleton implementations automatically with built-in validation for common concurrency issues.

## Quick Start

1. Add the `[Singleton]` attribute to your partial class
2. Make sure your class has a private constructor
3. The generator creates the singleton implementation automatically

```csharp
using CodeGenerator.Patterns.Singleton;

[Singleton(Strategy = SingletonStrategy.Lazy)]
public partial class ConfigurationManager
{
    private ConfigurationManager() { } // Private constructor required
    
    public string GetSetting(string key) { /* your code */ }
}

// Usage
var config = ConfigurationManager.Instance;
string setting = config.GetSetting("DatabaseTimeout");
```

## Singleton Strategies

Choose the right strategy based on your use case:

### 1. Lazy (Default) - `SingletonStrategy.Lazy`
**When to use**: Most general-purpose scenarios
**Performance**: Good initialization, good access speed
**Memory**: Minimal overhead

```csharp
[Singleton(Strategy = SingletonStrategy.Lazy)]
public partial class ConfigurationManager
{
    private readonly ConcurrentDictionary<string, string> _settings = new();
    private ConfigurationManager() { }
}
```

**Best for:**
- Configuration managers
- Service registries
- Non-performance-critical singletons

### 2. Eager - `SingletonStrategy.Eager`
**When to use**: When you need the instance ready immediately at startup
**Performance**: Fastest access (no initialization checks)
**Memory**: Instance created at application start

```csharp
[Singleton(Strategy = SingletonStrategy.Eager)]
public partial class Logger
{
    private readonly string _logFilePath;
    private Logger() 
    {
        _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
    }
}
```

**Best for:**
- Logging systems
- Critical infrastructure components
- Components needed immediately at startup

### 3. LockFree - `SingletonStrategy.LockFree`
**When to use**: High-performance scenarios with frequent access
**Performance**: Fastest for read-heavy workloads
**Memory**: Minimal locking overhead

```csharp
[Singleton(Strategy = SingletonStrategy.LockFree)]
public partial class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private MetricsCollector() { }
}
```

**Best for:**
- Metrics collection
- Caching systems
- High-frequency data access

### 4. DoubleCheckedLocking - `SingletonStrategy.DoubleCheckedLocking`
**When to use**: When you need precise control over initialization timing
**Performance**: Good balance of speed and memory efficiency
**Memory**: Standard locking overhead

```csharp
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class DbConnectionPool
{
    private readonly ConcurrentQueue<IDbConnection> _connections = new();
    private DbConnectionPool() { }
}
```

**Best for:**
- Database connection pools
- Resource managers
- Components with expensive initialization

## Thread Safety Validation

The generator automatically checks for common thread safety issues:

### ❌ Dangerous Collections
```csharp
[Singleton]
public partial class BadExample
{
    private Dictionary<string, int> _data = new(); // ❌ Will generate warning
    private List<string> _items = new();           // ❌ Will generate warning
}
```

### ✅ Thread-Safe Alternatives
```csharp
[Singleton]
public partial class GoodExample
{
    private ConcurrentDictionary<string, int> _data = new(); // ✅ Thread-safe
    private ConcurrentBag<string> _items = new();            // ✅ Thread-safe
}
```

## Generic Singletons

The generator supports generic singletons with constraints:

```csharp
[Singleton(Strategy = SingletonStrategy.DoubleCheckedLocking)]
public partial class Repository<T> where T : IEntity
{
    private readonly ConcurrentBag<T> _items = new();
    private Repository() { }
    
    public void Add(T item) => _items.Add(item);
    public IReadOnlyList<T> GetAll() => _items.ToList().AsReadOnly();
}

// Usage
var userRepo = Repository<User>.Instance;
var productRepo = Repository<Product>.Instance;
```

## Initialization Support

Add an `Initialize()` method for post-construction setup:

```csharp
[Singleton]
public partial class CacheManager
{
    private CacheManager() { }
    
    private void Initialize()
    {
        // Called automatically after instance creation
        _ = Task.Run(CleanupExpiredEntries);
        Console.WriteLine("Cache cleanup task started");
    }
}
```

## Requirements

- **Partial class**: Your class must be declared as `partial`
- **Private constructor**: Required to prevent external instantiation
- **.NET Framework 4.0+**: Compatible with legacy applications

## Common Patterns

### Configuration Management
```csharp
[Singleton(Strategy = SingletonStrategy.Lazy)]
public partial class AppConfig
{
    private readonly ConcurrentDictionary<string, string> _settings = new();
    private AppConfig() { LoadFromFile(); }
}
```

### Resource Pooling
```csharp
[Singleton(Strategy = SingletonStrategy.LockFree)]
public partial class ObjectPool<T>
{
    private readonly ConcurrentQueue<T> _objects = new();
    private ObjectPool() { PrePopulatePool(); }
}
```

### Metrics Collection
```csharp
[Singleton(Strategy = SingletonStrategy.LockFree)]
public partial class PerformanceCounters
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private PerformanceCounters() { }
}
```

## Warnings and Diagnostics

The generator provides helpful warnings:

| Warning | Meaning | Solution |
|---------|---------|----------|
| **Non-thread-safe field** | Using `Dictionary`, `List`, etc. | Use `ConcurrentDictionary`, `ConcurrentBag`, etc. |
| **Public constructor** | Constructor is publicly accessible | Make constructor private |
| **Class not partial** | Cannot generate singleton implementation | Add `partial` keyword |

## Performance Comparison

| Strategy | Initialization | Access Speed | Memory | Use Case |
|----------|---------------|--------------|---------|----------|
| **Lazy** | Lazy | Good | Low | General purpose |
| **Eager** | Immediate | Fastest | Higher | Critical systems |
| **LockFree** | Lazy | Fastest | Low | High-frequency access |
| **DoubleCheckedLocking** | Lazy | Good | Low | Balanced performance |

## Best Practices

### ✅ Do
- Use `ConcurrentDictionary` instead of `Dictionary`
- Use `ConcurrentBag` instead of `List`
- Make constructors private
- Use `Initialize()` method for setup logic
- Choose appropriate strategy for your use case

### ❌ Don't
- Use non-thread-safe collections as instance fields
- Make constructors public
- Forget the `partial` keyword
- Use singletons for everything (only when truly needed)

## Migration from Manual Singletons

**Before** (error-prone):
```csharp
public class MyService
{
    private static MyService _instance;
    private static readonly object _lock = new object();
    
    public static MyService Instance 
    {
        get 
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new MyService();
                }
            }
            return _instance;
        }
    }
}
```

**After** (generated, correct):
```csharp
[Singleton]
public partial class MyService
{
    private MyService() { }
    // All singleton logic generated automatically
}
```

## Troubleshooting

**Issue**: "Class must be partial"
**Solution**: Add `partial` keyword to your class declaration

**Issue**: "Public constructor warning"  
**Solution**: Make your constructor private

**Issue**: "Non-thread-safe collection warning"
**Solution**: Replace with thread-safe alternatives:
- `Dictionary` → `ConcurrentDictionary`
- `List` → `ConcurrentBag` or `ConcurrentQueue`
- `HashSet` → `ConcurrentDictionary<T, byte>`

---

*This generator helps create reliable singleton implementations for legacy .NET Framework applications where dependency injection is not available.*
