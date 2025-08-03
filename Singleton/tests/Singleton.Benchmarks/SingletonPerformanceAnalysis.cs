using BenchmarkDotNet.Attributes;

namespace Singleton.Benchmarks;

// BENCHMARK COMPARISON OF DIFFERENT SINGLETON STRATEGIES
[MemoryDiagnoser]
[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
public class SingletonPerformanceBenchmarks
{
    private const int IterationCount = 1000000;

    [Benchmark(Baseline = true)]
    public object EagerSingleton()
    {
        // Fastest - no synchronization needed on access
        return EagerSingletonExample.Instance;
    }

    [Benchmark]
    public object LockFreeSingleton()
    {
        // Very fast - uses Interlocked.CompareExchange, no locks
        return LockFreeSingletonExample.Instance;
    }

    [Benchmark]
    public object DoubleCheckedLockingSingleton()
    {
        // Fast - classic pattern with volatile and locks only during initialization
        return DoubleCheckedLockingExample.Instance;
    }

    [Benchmark]
    public object LazySingleton()
    {
        // Good performance - .NET's Lazy<T> implementation
        return LazySingletonExample.Instance;
    }

    [Benchmark]
    public object SimpleLockSingleton()
    {
        // Slowest - locks on every access (included for comparison)
        return SimpleLockSingletonExample.Instance;
    }

    // Concurrent access benchmark
    [Benchmark]
    public void ConcurrentLockFreeAccess()
    {
        Parallel.For(0, IterationCount, i =>
        {
            var instance = LockFreeSingletonExample.Instance;
            instance.DoWork();
        });
    }

    [Benchmark]
    public void ConcurrentEagerAccess()
    {
        Parallel.For(0, IterationCount, i =>
        {
            var instance = EagerSingletonExample.Instance;
            instance.DoWork();
        });
    }
}

// STRATEGY 1: EAGER INITIALIZATION (Fastest Access)
public class EagerSingletonExample
{
    private static readonly EagerSingletonExample _instance = new EagerSingletonExample();
    
    static EagerSingletonExample() { } // Explicit static constructor for beforefieldinit
    
    private EagerSingletonExample() 
    {
        // Initialize here - called once at type initialization
    }
    
    public static EagerSingletonExample Instance => _instance;
    
    public void DoWork() { /* Some work */ }
}

// STRATEGY 2: LOCK-FREE WITH INTERLOCKED (Best Balance)
public class LockFreeSingletonExample
{
    private static volatile LockFreeSingletonExample? _instance;
    private static int _isInitialized = 0;
    
    private LockFreeSingletonExample() 
    {
        // Initialize here
    }
    
    public static LockFreeSingletonExample Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path - no synchronization
            
            return GetOrCreateInstance();
        }
    }
    
    private static LockFreeSingletonExample GetOrCreateInstance()
    {
        if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) == 0)
        {
            // We won the race - create the instance
            var newInstance = new LockFreeSingletonExample();
            Interlocked.MemoryBarrier(); // Ensure initialization completes before assignment
            _instance = newInstance;
        }
        else
        {
            // Another thread is creating the instance - spin wait
            SpinWait.SpinUntil(() => _instance != null);
        }
        
        return _instance!;
    }
    
    public void DoWork() { /* Some work */ }
}

// STRATEGY 3: DOUBLE-CHECKED LOCKING (Classic High-Performance)
public class DoubleCheckedLockingExample
{
    private static volatile DoubleCheckedLockingExample? _instance;
    private static readonly object _lock = new object();
    
    private DoubleCheckedLockingExample() 
    {
        // Initialize here
    }
    
    public static DoubleCheckedLockingExample Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            
            lock (_lock)
            {
                if (_instance == null) // Double check inside lock
                {
                    _instance = new DoubleCheckedLockingExample();
                }
            }
            
            return _instance;
        }
    }
    
    public void DoWork() { /* Some work */ }
}

// STRATEGY 4: LAZY<T> (Good Performance, Simple)
public class LazySingletonExample
{
    private static readonly Lazy<LazySingletonExample> _lazy = 
        new Lazy<LazySingletonExample>(() => new LazySingletonExample());
    
    private LazySingletonExample() 
    {
        // Initialize here
    }
    
    public static LazySingletonExample Instance => _lazy.Value;
    
    public void DoWork() { /* Some work */ }
}

// STRATEGY 5: SIMPLE LOCK (Poor Performance - for comparison)
public class SimpleLockSingletonExample
{
    private static SimpleLockSingletonExample? _instance;
    private static readonly object _lock = new object();
    
    private SimpleLockSingletonExample() 
    {
        // Initialize here
    }
    
    public static SimpleLockSingletonExample Instance
    {
        get
        {
            lock (_lock) // Lock on every access - SLOW!
            {
                if (_instance == null)
                {
                    _instance = new SimpleLockSingletonExample();
                }
                return _instance;
            }
        }
    }
    
    public void DoWork() { /* Some work */ }
}

