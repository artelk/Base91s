using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Base91s;

public static partial class Base91
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8,
        out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
    {
        if (!isFinalBlock)
        {
            if (bytes.Length < 13)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return;
            }

            if ((uint)bytes.Length > MaximumEncodeLength)
                ThrowLengthGreaterThanMaximumEncodeLength(bytes.Length);

            var blockCount = (uint)bytes.Length / 13;
            var utf8Length = (int)(blockCount * 16);

            if (utf8.Length < utf8Length)
                ThrowDestinationTooShort(utf8Length, utf8.Length);

            EncodeBlocks(bytes, utf8, blockCount);

            bytesConsumed = (int)(blockCount * 13);
            bytesWritten = utf8Length;
        }
        else
        {
            EncodeFinal(bytes, utf8, out bytesConsumed, out bytesWritten);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void EncodeFinal(ReadOnlySpan<byte> bytes, Span<byte> utf8,
        out int bytesConsumed, out int bytesWritten)
    {
        if ((uint)bytes.Length > MaximumEncodeLength)
            ThrowLengthGreaterThanMaximumEncodeLength(bytes.Length);

        var (d, r) = Math.DivRem((uint)bytes.Length, 13);
        var utf8Length = (int)(d * 16 + r + (r > 0).ToByte() + (r > 4).ToByte() + (r > 8).ToByte());

        if (utf8.Length < utf8Length)
            ThrowDestinationTooShort(utf8Length, utf8.Length);

        ref var src = ref MemoryMarshal.GetReference(bytes);
        ref var dst = ref MemoryMarshal.GetReference(utf8);

        if (d != 0)
        {
            EncodeBlocks(ref src, ref dst, d);
            src = ref src.Add(d * 13);
            dst = ref dst.Add(d * 16);
        }

        if (r != 0)
            EncodeReminder(ref src, ref dst, r);

        bytesConsumed = bytes.Length;
        bytesWritten = utf8Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeBlocks(ReadOnlySpan<byte> bytes, Span<byte> utf8, uint blockCount)
    {
        EncodeBlocks(ref MemoryMarshal.GetReference(bytes), ref MemoryMarshal.GetReference(utf8), blockCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeReminder(ref byte src, ref byte dst, uint reminderLength)
    {
        switch (reminderLength)
        {
            case 1:
                Encode1Byte(ref src, ref dst);
                return;
            case 2:
                Encode2Bytes(ref src, ref dst);
                return;
            case 3:
                Encode3Bytes(ref src, ref dst);
                return;
            case 4:
                Encode4Bytes(ref src, ref dst);
                return;
            case 5:
                Encode5Bytes(ref src, ref dst);
                return;
            case 6:
                Encode6Bytes(ref src, ref dst);
                return;
            case 7:
                Encode7Bytes(ref src, ref dst);
                return;
            case 8:
                Encode8Bytes(ref src, ref dst);
                return;
            case 9:
                Encode9Bytes(ref src, ref dst);
                return;
            case 10:
                Encode10Bytes(ref src, ref dst);
                return;
            case 11:
                Encode11Bytes(ref src, ref dst);
                return;
            case 12:
                Encode12Bytes(ref src, ref dst);
                return;
            default:
                return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void EncodeBlocks(ref byte src, ref byte dst, uint blockCount)
    {
        if (Use52BytesBlocksOnEncoding)
        {
            if (blockCount >= 4)
            {
                do
                {
                    Encode52Bytes(ref src, ref dst);
                    src = ref src.Add(52);
                    dst = ref dst.Add(64);
                    blockCount -= 4;
                } while (blockCount >= 4);
            }

            if (blockCount >= 2)
            {
                Encode26Bytes(ref src, ref dst);
                blockCount -= 2;

                if (blockCount != 0)
                {
                    src = ref src.Add(26);
                    dst = ref dst.Add(32);
                    Encode13Bytes(ref src, ref dst);
                }
            }
            else if (blockCount != 0)
            {
                Encode13Bytes(ref src, ref dst);
            }
        }
        else
        {
            if (blockCount >= 2)
            {
                do
                {
                    Encode26Bytes(ref src, ref dst);
                    src = ref src.Add(26);
                    dst = ref dst.Add(32);
                    blockCount -= 2;
                } while (blockCount >= 2);
            }

            if (blockCount != 0)
            {
                Encode13Bytes(ref src, ref dst);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode52Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = (src.Add(5).ReadULong() >> 12).Split();
        ulong b3 = src.Add(13).ReadULong().Split();
        ulong b4 = (src.Add(13 + 5).ReadULong() >> 12).Split();

        ulong b5 = src.Add(26).ReadULong().Split();
        ulong b6 = (src.Add(26 + 5).ReadULong() >> 12).Split();
        ulong b7 = src.Add(26 + 13).ReadULong().Split();
        ulong b8 = (src.Add(26 + 13 + 5).ReadULong() >> 12).Split();

        EncodeTo(b1, b2, b3, b4, b5, b6, b7, b8, ref dst, 64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode26Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = (src.Add(5).ReadULong() >> 12).Split();
        ulong b3 = src.Add(13).ReadULong().Split();
        ulong b4 = (src.Add(13 + 5).ReadULong() >> 12).Split();
        EncodeTo(b1, b2, b3, b4, ref dst, 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode13Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = (src.Add(5).ReadULong() >> 12).Split();
        EncodeTo(b1, b2, ref dst, 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode12Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = (src.Add(4).ReadULong() >> 20).Split();
        EncodeTo(b1, b2, ref dst, 15);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode11Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = (src.Add(3).ReadULong() >> 28).Split();
        EncodeTo(b1, b2, ref dst, 14);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode10Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        ulong b2 = ((ulong)src.Add(6).ReadUInt() >> 4).Split();
        EncodeTo(b1, b2, ref dst, 13);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode9Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = src.ReadULong().Split();
        uint b2 = (src.Add(5).ReadUInt() >> 12).Split();
        EncodeTo(b1, b2, ref dst, 12);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode8Bytes(ref byte src, ref byte dst)
    {
        ulong b = src.ReadULong();
        ulong b1 = b.Split();
        ulong b2 = b >> 52;
        EncodeTo(b1, b2, ref dst, 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode7Bytes(ref byte src, ref byte dst)
    {
        ulong b = (ulong)src.ReadUInt() | ((ulong)src.Add(3).ReadUInt() << 24);
        ulong b1 = b.Split();
        ulong b2 = b >> 52;
        EncodeTo(b1, b2, ref dst, 9);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode6Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = ((ulong)src.ReadUInt() | ((ulong)src.Add(4).ReadUShort() << 32)).Split();
        EncodeTo(b1, ref dst, 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode5Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = ((ulong)src.ReadUInt() | ((ulong)src.Add(4).ReadByte() << 32)).Split();
        EncodeTo(b1, ref dst, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode4Bytes(ref byte src, ref byte dst)
    {
        ulong b1 = ((ulong)src.ReadUInt()).Split();
        EncodeTo(b1, ref dst, 5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode3Bytes(ref byte src, ref byte dst)
    {
        uint b1 = ((uint)src.ReadUShort() | ((uint)src.Add(2).ReadByte() << 16)).Split();
        EncodeTo(b1, ref dst, 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode2Bytes(ref byte src, ref byte dst)
    {
        uint b1 = ((uint)src.ReadUShort()).Split();
        EncodeTo(b1, ref dst, 3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Encode1Byte(ref byte src, ref byte dst)
    {
        ushort b1 = src.ReadByte();
        EncodeTo(b1, ref dst, 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(ulong b1, ulong b2, ulong b3, ulong b4,
                                 ulong b5, ulong b6, ulong b7, ulong b8,
                                 ref byte dst, [ConstantExpected(Min = 0, Max = 64)] uint length)
    {
        Debug.Assert(length <= 64);

        if (!DisableV512 && Vector512.IsHardwareAccelerated && Avx512BW.IsSupported)
            dst.Write(EncodeVector512(b1, b2, b3, b4, b5, b6, b7, b8), length);
        else
        {
            if (length >= 32)
            {
                EncodeTo(b1, b2, b3, b4, ref dst, 32);
                length -= 32;
                if (length > 0)
                    EncodeTo(b5, b6, b7, b8, ref dst.Add(32), length);
            }
            else
                EncodeTo(b1, b2, b3, b4, ref dst, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(ulong b1, ulong b2, ulong b3, ulong b4,
                                 ref byte dst, [ConstantExpected(Min = 0, Max = 32)] uint length)
    {
        Debug.Assert(length <= 32);

        if (!DisableV256 && Vector256.IsHardwareAccelerated && (Avx2.IsSupported || Vector512.IsHardwareAccelerated))
            dst.Write(EncodeVector256(b1, b2, b3, b4), length);
        else
        {
            if (length >= 16)
            {
                EncodeTo(b1, b2, ref dst, 16);
                length -= 16;
                if (length > 0)
                    EncodeTo(b3, b4, ref dst.Add(16), length);
            }
            else
                EncodeTo(b1, b2, ref dst, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(ulong b1, ulong b2,
                                 ref byte dst, [ConstantExpected(Max = 16)] uint length)
    {
        Debug.Assert(length <= 16);

        if (!DisableV128 && Vector128.IsHardwareAccelerated && (Sse2.IsSupported || AdvSimd.IsSupported || Vector256.IsHardwareAccelerated))
            dst.Write(EncodeVector128(b1, b2), length);
        else
        {
            if (length >= 8)
            {
                EncodeTo(b1, ref dst, 8);
                length -= 8;
                if (length > 0)
                    EncodeTo(b2, ref dst.Add(8), length);
            }
            else
                EncodeTo(b1, ref dst, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(ulong b1, ref byte dst, [ConstantExpected(Max = 8)] uint length)
    {
        Debug.Assert(length <= 8);

        if (!DisableV64 && Vector64.IsHardwareAccelerated && (AdvSimd.IsSupported || Vector128.IsHardwareAccelerated))
            dst.Write(EncodeVector64(b1), length);
        else if (!DisableV128 && Vector128.IsHardwareAccelerated && (Sse2.IsSupported || AdvSimd.IsSupported || Vector256.IsHardwareAccelerated))
            dst.Write(EncodeVector128(b1, 0), length);
        else
            dst.Write(EncodeULong(b1), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(uint b1, ref byte dst, [ConstantExpected(Max = 4)] uint length)
    {
        Debug.Assert(length <= 4);
        if (!DisableV64 && Vector64.IsHardwareAccelerated && (AdvSimd.IsSupported || Vector128.IsHardwareAccelerated))
            dst.Write(EncodeVector64(b1), length);
        else if (!DisableV128 && Vector128.IsHardwareAccelerated && (Sse2.IsSupported || AdvSimd.IsSupported || Vector256.IsHardwareAccelerated))
            dst.Write(EncodeVector128(b1, 0), length);
        else
            dst.Write(EncodeUInt(b1), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeTo(ushort b1, ref byte dst, [ConstantExpected(Max = 2)] uint length)
    {
        Debug.Assert(length <= 2);
        dst.Write(EncodeUShort(b1), length);
    }

    private const ushort Div91Mul = (1 << 18) / 91 + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector512<byte> EncodeVector512(ulong b1, ulong b2, ulong b3, ulong b4,
                                                   ulong b5, ulong b6, ulong b7, ulong b8)
    {
        if (!Vector512.IsHardwareAccelerated)
            ThrowNotSupported();
        var v = Vector512.Create(b1, b2, b3, b4, b5, b6, b7, b8).AsUInt16();
        Unsafe.SkipInit(out Vector512<ushort> q);
        if (Avx512BW.IsSupported)
            q = Avx512BW.MultiplyHigh(v, Vector512.Create<ushort>(Div91Mul)) >>> 2;
        else
            ThrowNotSupported();
        var r = v - q * 91;

        var result = (r | (q << 8)).AsByte();
        result += Vector512.Create((byte)0x23);
        result = Vector512.ConditionalSelect(
            Vector512.Equals(result, Vector512.Create((byte)0x5C)),
            Vector512.Create((byte)0x7E),
            result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> EncodeVector256(ulong b1, ulong b2, ulong b3, ulong b4)
    {
        if (!Vector256.IsHardwareAccelerated)
            ThrowNotSupported();
        var v = Vector256.Create(b1, b2, b3, b4).AsUInt16();
        Unsafe.SkipInit(out Vector256<ushort> q);
        if (Avx2.IsSupported)
            q = Avx2.MultiplyHigh(v, Vector256.Create<ushort>(Div91Mul)) >>> 2;
        else if (Vector512.IsHardwareAccelerated)
            q = ((v.Widen() * Div91Mul) >> 18).Narrow();
        else
            ThrowNotSupported();
        var r = v - q * 91;

        var result = (r | (q << 8)).AsByte();
        result += Vector256.Create((byte)0x23);
        result = Vector256.ConditionalSelect(
            Vector256.Equals(result, Vector256.Create((byte)0x5C)),
            Vector256.Create((byte)0x7E),
            result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> EncodeVector128(ulong b1, ulong b2)
    {
        if (!Vector128.IsHardwareAccelerated)
            ThrowNotSupported();
        var v = Vector128.Create(b1, b2).AsUInt16();
        Unsafe.SkipInit(out Vector128<ushort> q);
        if (Sse2.IsSupported)
            q = Sse2.MultiplyHigh(v, Vector128.Create<ushort>(Div91Mul)) >>> 2;
        else if (AdvSimd.IsSupported)
            q = AdvSimd.MultiplyDoublingByScalarSaturateHigh(v.AsInt16(),
                         Vector64.CreateScalarUnsafe<short>((short)Div91Mul)).AsUInt16() >>> 3;
        else if (Vector256.IsHardwareAccelerated)
            q = ((v.Widen() * Div91Mul) >> 18).Narrow();
        else
            ThrowNotSupported();
        var r = v - q * 91;

        var result = (r | (q << 8)).AsByte();
        result += Vector128.Create((byte)0x23);
        result = Vector128.ConditionalSelect(
            Vector128.Equals(result, Vector128.Create((byte)0x5C)),
            Vector128.Create((byte)0x7E),
            result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector64<byte> EncodeVector64(ulong b1)
    {
        if (!Vector64.IsHardwareAccelerated)
            ThrowNotSupported();
        var v = Vector64.Create(b1).AsUInt16();
        Unsafe.SkipInit(out Vector64<ushort> q);
        if (AdvSimd.IsSupported)
            q = AdvSimd.MultiplyDoublingByScalarSaturateHigh(v.AsInt16(),
                         Vector64.CreateScalarUnsafe<short>((short)Div91Mul)).AsUInt16() >>> 3;
        else if (Vector128.IsHardwareAccelerated)
            q = ((v.Widen() * Div91Mul) >> 18).Narrow();
        else
            ThrowNotSupported();
        var r = v - q * 91;

        var result = (r | (q << 8)).AsByte();
        result += Vector64.Create((byte)0x23);
        result = Vector64.ConditionalSelect(
            Vector64.Equals(result, Vector64.Create((byte)0x5C)),
            Vector64.Create((byte)0x7E),
            result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeULong(ulong b1)
    {
        return EncodeUInt((uint)b1) | ((ulong)EncodeUInt((uint)(b1 >> 32)) << 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint EncodeUInt(uint b1)
    {
        return EncodeUShort((ushort)b1) | ((uint)EncodeUShort((ushort)(b1 >> 16)) << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort EncodeUShort(ushort b1)
    {
        uint v = b1;
        uint q = (v * Div91Mul) >>> 18;
        uint r = v - q * 91;
        q += 0x23;
        r += 0x23;
        q = (q == 0x5C) ? 0x7E : q;
        r = (r == 0x5C) ? 0x7E : r;
        return (ushort)(r | (q << 8));
    }
}
