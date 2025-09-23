using System.Collections.Concurrent;
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace Singleton.UnitTests.UseCases;

// HIGH-PERFORMANCE SINGLETON - Lock-free access for read-heavy scenarios
[Singleton(Strategy = SingletonStrategy.LockFree)]
public partial class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();
    
    private MetricsCollector()
    {
    }

    private void Initialize()
    {
        Console.WriteLine("MetricsCollector initialized");
    }

    public void IncrementCounter(string name) => _counters.AddOrUpdate(name, 1, (key, value) => value + 1);
    public void SetGauge(string name, double value) => _gauges.AddOrUpdate(name, value, (key, oldValue) => value);
    public long GetCounter(string name) => _counters.GetValueOrDefault(name, 0);
    public double GetGauge(string name) => _gauges.GetValueOrDefault(name, 0.0);
}

//GENERATED CONVERSION TO SINGLETON
partial class MetricsCollector
{
    private static volatile MetricsCollector? _instance;
    private static int _isInitialized = 0;

    public static MetricsCollector Instance
    {
        get
        {
            if (_instance != null) return _instance; // Fast path
            return GetOrCreateInstance();
        }
    }

    private static MetricsCollector GetOrCreateInstance()
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

    private static MetricsCollector CreateSingletonInstance()
    {
        var instance = new MetricsCollector();
        instance.Initialize();
        return instance;
    }
}