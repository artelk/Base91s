using BenchmarkDotNet.Attributes;
using System.Buffers.Text;

namespace Base91s.Benchmark;

public class EncodingDecodingBenchmark
{
    private static byte[] input = new byte[10_000_000];
    private static byte[] encoded = new byte[input.Length + input.Length / 4 + 3];
    private static byte[] decoded = new byte[input.Length];

    [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 20, 26, 52,
            100, 1000, 10_000, 100_000, 1000_000, 10_000_000)]
    public int Length { get; set; }

    static EncodingDecodingBenchmark()
    {
        Random.Shared.NextBytes(input);
    }

    [Benchmark(Baseline = true)]
    public void EncodeDecodeBase64()
    {
        Base64.EncodeToUtf8(input.AsSpan(0, Length), encoded, out var bytesConsumed, out var bytesWritten, true);
        Base64.DecodeFromUtf8(encoded.AsSpan(0, bytesWritten), decoded, out var _, out var _, true);
    }

    [Benchmark]
    public void EncodeDecodeBase91()
    {
        Base91.EncodeToUtf8(input.AsSpan(0, Length), encoded, out var bytesConsumed, out var bytesWritten, true);
        Base91.DecodeFromUtf8(encoded.AsSpan(0, bytesWritten), decoded, out var _, out var _, true);
    }
}
