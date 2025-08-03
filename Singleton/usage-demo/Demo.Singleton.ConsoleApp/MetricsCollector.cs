using System.Collections.Concurrent;
using CodeGenerator.Patterns.Singleton;

namespace Demo.Singleton.ConsoleApp;

// HIGH-PERFORMANCE SINGLETON - Lock-free access for read-heavy scenarios
[Singleton(Strategy = SingletonStrategy.LockFree, LazyInitialization = true)]
public partial class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, double> _gauges = new();

    private void Initialize()
    {
        Console.WriteLine("MetricsCollector initialized");
    }

    public void IncrementCounter(string name) => _counters.AddOrUpdate(name, 1, (key, value) => value + 1);
    public void SetGauge(string name, double value) => _gauges.AddOrUpdate(name, value, (key, oldValue) => value);
    public long GetCounter(string name) => _counters.GetValueOrDefault(name, 0);
    public double GetGauge(string name) => _gauges.GetValueOrDefault(name, 0.0);
}