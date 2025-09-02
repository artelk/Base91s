using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Base91s;

public static partial class Base91
{
    public const int MaximumEncodeLength = 1_744_830_463;

    private const bool DisableV64 = false;
    private const bool DisableV128 = false;
    private const bool DisableV256 = false;
    private const bool DisableV512 = false;

    private const bool Use52BytesBlocksOnEncoding = true;
    private const bool Use64BytesBlocksOnDecoding = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetEncodedToUtf8Length(int length)
    {
        if ((uint)length > MaximumEncodeLength)
            ThrowLengthGreaterThanMaximumEncodeLength(length);
        if (length == 0)
            return 0;

        var (d, r) = Math.DivRem((uint)length, 13);
        return (int)(d * 16 + r + (r > 0).ToByte() + (r > 4).ToByte() + (r > 8).ToByte());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? GetDecodedFromUtf8Length(int length)
    {
        if (length < 0)
            ThrowLengthNegative(length);

        var d = (uint)length / 16;
        var r = (uint)length % 16;
        if (r == 1 | r == 6 | r == 11)
            return null;

        return (int)(d * 13 + r - (r > 0).ToByte() - (r > 5).ToByte() - (r > 10).ToByte());
    }

    public static bool IsValid(ReadOnlySpan<byte> base91TextUtf8)
    {
        var r = (uint)base91TextUtf8.Length % 16;
        if (r == 1 | r == 6 | r == 11)
            return false;
        return !base91TextUtf8.ContainsAnyExceptInRange<byte>(0x23, 0x7E);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowLengthGreaterThanMaximumEncodeLength(int length)
        => throw new ArgumentOutOfRangeException(nameof(length), length, "Encoding span length must be in range 0..MaximumEncodeLength");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowLengthNegative(int length)
        => throw new ArgumentOutOfRangeException(nameof(length), length, "Length must not be negative");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowInvalidLength()
        => throw new ArgumentException("Invalid length of decoding span");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowDestinationTooShort(int minLength, int length)
        => throw new ArgumentOutOfRangeException(nameof(length), length, $"Destination span is too short. Required at least {minLength} bytes.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowNotSupported()
        => throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    [StackTraceHidden]
    private static void ThrowArgumentOutOfRange()
        => throw new ArgumentOutOfRangeException();
}
