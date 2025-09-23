using System.Collections.Concurrent;
using Knara.SourceGenerators.DesignPatterns.Singleton;

namespace Demo.Singleton.DotNetCore;

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