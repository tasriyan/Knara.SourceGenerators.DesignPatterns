using BenchmarkDotNet.Running;

namespace Singleton.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<SingletonGeneratorBenchmarks>();
        BenchmarkRunner.Run<SingletonPerformanceBenchmarks>();
        BenchmarkRunner.Run<SingletonFeatureBenchmarks>();
    }
}