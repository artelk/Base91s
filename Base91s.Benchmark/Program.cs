using BenchmarkDotNet.Running;

namespace Base91s.Benchmark;

internal class Program
{
    static void Main(string[] args)
    {
        //BenchmarkRunner.Run<EncodingBenchmark>();
        BenchmarkRunner.Run<EncodingDecodingBenchmark>();
        //BenchmarkRunner.Run<EncodingDecoding1to20Benchmark>();
    }
}
