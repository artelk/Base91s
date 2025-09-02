using BenchmarkDotNet.Attributes;
using System.Buffers.Text;

namespace Base91s.Benchmark;

public class EncodingDecoding1to20Benchmark
{
    private static byte[] input = new byte[20];
    private static byte[] encoded = new byte[30];
    private static byte[] decoded = new byte[20];

    static EncodingDecoding1to20Benchmark()
    {
        Random.Shared.NextBytes(input);
    }

    [Benchmark(Baseline = true)]
    public void EncodeDecodeBase64()
    {
        for (int length = 1; length <= 20; length++)
        {
            Base64.EncodeToUtf8(input.AsSpan(0, length), encoded, out var bytesConsumed, out var bytesWritten, true);
            Base64.DecodeFromUtf8(encoded.AsSpan(0, bytesWritten), decoded, out bytesConsumed, out bytesWritten, true);
        }
    }

    [Benchmark]
    public void EncodeDecodeBase91()
    {
        for (int length = 1; length <= 20; length++)
        {
            Base91.EncodeToUtf8(input.AsSpan(0, length), encoded, out var bytesConsumed, out var bytesWritten, true);
            Base91.DecodeFromUtf8(encoded.AsSpan(0, bytesWritten), decoded, out bytesConsumed, out bytesWritten, true);
        }
    }
}
