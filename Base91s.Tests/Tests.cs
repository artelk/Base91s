using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace Base91s.Tests;

public class Tests
{
    static Tests()
    {
        Console.Error.WriteLine($"Vector64.IsHardwareAccelerated = {Vector64.IsHardwareAccelerated}");
        Console.Error.WriteLine($"Vector128.IsHardwareAccelerated = {Vector128.IsHardwareAccelerated}");
        Console.Error.WriteLine($"Vector256.IsHardwareAccelerated = {Vector256.IsHardwareAccelerated}");
        Console.Error.WriteLine($"Vector512.IsHardwareAccelerated = {Vector512.IsHardwareAccelerated}");
        Console.Error.WriteLine($"Sse41.IsSupported = {Sse41.IsSupported}");
        Console.Error.WriteLine($"Bmi2.X64.IsSupported = {Bmi2.X64.IsSupported}");
        Console.Error.WriteLine($"Avx2.IsSupported = {Avx2.IsSupported}");
        Console.Error.WriteLine($"Avx512F.IsSupported = {Avx512F.IsSupported}");
        Console.Error.WriteLine($"Avx512F.VL.IsSupported = {Avx512F.VL.IsSupported}");
        Console.Error.WriteLine($"Avx512BW.IsSupported = {Avx512BW.IsSupported}");
        Console.Error.WriteLine($"AdvSimd.IsSupported = {AdvSimd.IsSupported}");
    }

    [Test]
    public void TestFinalBlock()
    {
        for (int length = 0; length <= 1000; length++)
        {
            var input = new byte[length];
            var encodedLength = Base91.GetEncodedToUtf8Length(length);
            var encoded = new byte[encodedLength + 10];
            RandomNumberGenerator.Fill(input);
            encoded.AsSpan().Fill(0xAA);

            Base91.EncodeToUtf8(input, encoded, out var bytesConsumed, out var bytesWritten, true);
            Assert.That(bytesConsumed, Is.EqualTo(length));
            Assert.That(bytesWritten, Is.EqualTo(encodedLength));
            Assert.That(Base91.GetDecodedFromUtf8Length(bytesWritten), Is.EqualTo(length));
            Assert.That(!encoded.AsSpan(encodedLength).ContainsAnyExcept((byte)0xAA));

            var decoded = new byte[length + 10];
            decoded.AsSpan().Fill(0xAA);
            Base91.DecodeFromUtf8(encoded.AsSpan(0, encodedLength), decoded, out bytesConsumed, out bytesWritten, true);
            Assert.That(bytesConsumed, Is.EqualTo(encodedLength));
            Assert.That(bytesWritten, Is.EqualTo(length));
            Assert.That(!decoded.AsSpan(length).ContainsAnyExcept((byte)0xAA));
            Assert.That(decoded.AsSpan(0, length).SequenceEqual(input));
        }
    }

    [Test]
    public void TestNotFinalBlock()
    {
        var input = new byte[1001].AsSpan();
        var encoded = new byte[Base91.GetEncodedToUtf8Length(input.Length)].AsSpan();
        var decoded = new byte[input.Length].AsSpan();
        RandomNumberGenerator.Fill(input);

        for (int length = 0; length <= 1000; length++)
        {
            encoded.Fill(0xAA);
            decoded.Fill(0xAA);

            Base91.EncodeToUtf8(input[..length], encoded, out var encBytesConsumed, out var encBytesWritten, false);
            Assert.That(encBytesConsumed, Is.EqualTo(length - (length % 13)));
            Assert.That(encBytesWritten, Is.EqualTo((length / 13) * 16));

            Base91.DecodeFromUtf8(encoded[..encBytesWritten], decoded, out var decBytesConsumed, out var decBytesWritten, false);
            Assert.That(decBytesConsumed, Is.EqualTo(encBytesWritten));
            Assert.That(decBytesWritten, Is.EqualTo(encBytesConsumed));
            Assert.That(decoded[..decBytesWritten].SequenceEqual(input[..encBytesConsumed]));
        }
    }
}
