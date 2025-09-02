using BenchmarkDotNet.Attributes;
using System.Buffers.Text;

namespace Base91s.Benchmark;

public class EncodingBenchmark
{
    private static byte[] input = new byte[10_000_000];
    private static byte[] output = new byte[input.Length + input.Length / 4 + 3];

    [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 26, 52,
            100, 1000, 10_000, 100_000, 1000_000, 10_000_000)]
    public int Length { get; set; }

    static EncodingBenchmark()
    {
        Random.Shared.NextBytes(input);
    }

    [Benchmark(Baseline = true)]
    public void EncodeToBase64()
    {
        Base64.EncodeToUtf8(input.AsSpan(0, Length), output, out var _, out var _, true);
    }

    [Benchmark]
    public void EncodeToBase91()
    {
        Base91.EncodeToUtf8(input.AsSpan(0, Length), output, out var _, out var _, true);
    }
}
