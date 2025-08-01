using BenchmarkDotNet.Running;

namespace Builder.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<GeneratorBenchmarks>();
        BenchmarkRunner.Run<GeneratorHelpersBenchmarks>();
    }
}