using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Base91s;

public static partial class Base91
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes,
        out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
    {
        if (!isFinalBlock)
        {
            if (utf8.Length < 16)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return;
            }
            var blockCount = (uint)utf8.Length / 16;
            var bytesLength = (int)(blockCount * 13);
            if (bytes.Length < bytesLength)
                ThrowDestinationTooShort(bytesLength, utf8.Length);
            DecodeBlocks(utf8, bytes, blockCount);
            bytesConsumed = (int)(blockCount * 16);
            bytesWritten = bytesLength;
        }
        else
        {
            DecodeFinal(utf8, bytes, out bytesConsumed, out bytesWritten);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void DecodeFinal(ReadOnlySpan<byte> utf8, Span<byte> bytes,
        out int bytesConsumed, out int bytesWritten)
    {
        var (d, r) = Math.DivRem((uint)utf8.Length, 16);
        if (r == 1 | r == 6 | r == 11)
            ThrowInvalidLength();
        var bytesLength = (int)(d * 13 + r - (r > 0).ToByte() - (r > 5).ToByte() - (r > 10).ToByte());
        if (bytes.Length < bytesLength)
            ThrowDestinationTooShort(bytesLength, bytes.Length);
        ref var src = ref MemoryMarshal.GetReference(utf8);
        ref var dst = ref MemoryMarshal.GetReference(bytes);
        if (d != 0)
        {
            DecodeBlocks(ref src, ref dst, d);
            src = ref src.Add(d * 16);
            dst = ref dst.Add(d * 13);
        }
        if (r != 0)
            DecodeReminder(ref src, ref dst, r);
        bytesConsumed = utf8.Length;
        bytesWritten = bytesLength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DecodeBlocks(ReadOnlySpan<byte> utf8, Span<byte> bytes, uint blockCount)
    {
        DecodeBlocks(ref MemoryMarshal.GetReference(utf8), ref MemoryMarshal.GetReference(bytes), blockCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static void DecodeBlocks(ref byte src, ref byte dst, uint blockCount)
    {
        if (Use64BytesBlocksOnDecoding)
        {
            if (blockCount >= 4)
            {
                do
                {
                    Decode64Bytes(ref src, ref dst);
                    src = ref src.Add(64);
                    dst = ref dst.Add(32);
                    blockCount -= 4;
                } while (blockCount >= 4);
            }

            if (blockCount >= 2)
            {
                Decode32Bytes(ref src, ref dst);
                blockCount -= 2;

                if (blockCount != 0)
                {
                    src = ref src.Add(32);
                    dst = ref dst.Add(26);
                    Decode16Bytes(ref src, ref dst);
                }
            }
            else if (blockCount != 0)
            {
                Decode16Bytes(ref src, ref dst);
            }
        }
        else
        {
            if (blockCount >= 2)
            {
                do
                {
                    Decode32Bytes(ref src, ref dst);
                    src = ref src.Add(32);
                    dst = ref dst.Add(26);
                    blockCount -= 2;
                } while (blockCount >= 2);
            }

            if (blockCount != 0)
            {
                Decode16Bytes(ref src, ref dst);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DecodeReminder(ref byte src, ref byte dst, uint reminderLength)
    {
        switch (reminderLength)
        {
            case 2:
                DecodeBytes(ref src, ref dst, 2, 1);
                return;
            case 3:
                DecodeBytes(ref src, ref dst, 3, 2);
                return;
            case 4:
                DecodeBytes(ref src, ref dst, 4, 3);
                return;
            case 5:
                DecodeBytes(ref src, ref dst, 5, 4);
                return;
            case 7:
                DecodeBytes(ref src, ref dst, 7, 5);
                return;
            case 8:
                DecodeBytes(ref src, ref dst, 8, 6);
                return;
            case 9:
                DecodeBytes(ref src, ref dst, 9, 7);
                return;
            case 10:
                DecodeBytes(ref src, ref dst, 10, 8);
                return;
            case 12:
                DecodeBytes(ref src, ref dst, 12, 9);
                return;
            case 13:
                DecodeBytes(ref src, ref dst, 13, 10);
                return;
            case 14:
                DecodeBytes(ref src, ref dst, 14, 11);
                return;
            case 15:
                DecodeBytes(ref src, ref dst, 15, 12);
                return;
            default:
                return;
        }
    }

    private const ulong UL23s = 0x23232323_23232323;
    private const uint UI23s = 0x23232323;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Decode64Bytes(ref byte src, ref byte dst)
    {
        if (!DisableV512 && Vector512.IsHardwareAccelerated)
        {
            var bytes = src.ReadVector512() - Vector512.Create<byte>(0x23);
            Vector512<ulong> b = Decode(bytes);
            MergeAndWriteULongs(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], ref dst);
        }
        else if (!DisableV256 && Vector256.IsHardwareAccelerated)
        {
            var v23s = Vector256.Create<byte>(0x23);
            var bytes1 = src.ReadVector256() - v23s;
            var bytes2 = src.Add(32).ReadVector256() - v23s;
            Vector256<ulong> b1 = Decode(bytes1);
            Vector256<ulong> b2 = Decode(bytes2);
            MergeAndWriteULongs(b1[0], b1[1], b1[2], b1[3], b2[0], b2[1], b2[2], b2[3], ref dst);
        }
        else if (!DisableV128 && Vector128.IsHardwareAccelerated)
        {
            var v23s = Vector128.Create<byte>(0x23);
            var bytes1 = src.ReadVector128() - v23s;
            var bytes2 = src.Add(16).ReadVector128() - v23s;
            var bytes3 = src.Add(32).ReadVector128() - v23s;
            var bytes4 = src.Add(48).ReadVector128() - v23s;
            Vector128<ulong> b1 = Decode(bytes1);
            Vector128<ulong> b2 = Decode(bytes2);
            Vector128<ulong> b3 = Decode(bytes3);
            Vector128<ulong> b4 = Decode(bytes4);
            MergeAndWriteULongs(b1[0], b1[1], b2[0], b2[1], b3[0], b3[1], b4[0], b4[1], ref dst);
        }
        else if (!DisableV64 && Vector64.IsHardwareAccelerated)
        {
            var v23s = Vector64.Create<byte>(0x23);
            var bytes1 = src.ReadVector64() - v23s;
            var bytes2 = src.Add(8).ReadVector64() - v23s;
            var bytes3 = src.Add(16).ReadVector64() - v23s;
            var bytes4 = src.Add(24).ReadVector64() - v23s;
            var bytes5 = src.Add(32).ReadVector64() - v23s;
            var bytes6 = src.Add(40).ReadVector64() - v23s;
            var bytes7 = src.Add(48).ReadVector64() - v23s;
            var bytes8 = src.Add(56).ReadVector64() - v23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            ulong b3 = Decode(bytes3);
            ulong b4 = Decode(bytes4);
            ulong b5 = Decode(bytes5);
            ulong b6 = Decode(bytes6);
            ulong b7 = Decode(bytes7);
            ulong b8 = Decode(bytes8);
            MergeAndWriteULongs(b1, b2, b3, b4, b5, b6, b7, b8, ref dst);
        }
        else
        {
            var bytes1 = src.ReadULong() - UL23s;
            var bytes2 = src.Add(8).ReadULong() - UL23s;
            var bytes3 = src.Add(16).ReadULong() - UL23s;
            var bytes4 = src.Add(24).ReadULong() - UL23s;
            var bytes5 = src.Add(32).ReadULong() - UL23s;
            var bytes6 = src.Add(40).ReadULong() - UL23s;
            var bytes7 = src.Add(48).ReadULong() - UL23s;
            var bytes8 = src.Add(56).ReadULong() - UL23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            ulong b3 = Decode(bytes3);
            ulong b4 = Decode(bytes4);
            ulong b5 = Decode(bytes5);
            ulong b6 = Decode(bytes6);
            ulong b7 = Decode(bytes7);
            ulong b8 = Decode(bytes8);
            MergeAndWriteULongs(b1, b2, b3, b4, b5, b6, b7, b8, ref dst);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Decode32Bytes(ref byte src, ref byte dst)
    {
        if (!DisableV256 && Vector256.IsHardwareAccelerated)
        {
            var bytes = src.ReadVector256() - Vector256.Create<byte>(0x23);
            Vector256<ulong> b = Decode(bytes);
            MergeAndWriteULongs(b[0], b[1], b[2], b[3], ref dst);
        }
        else if (!DisableV128 && Vector128.IsHardwareAccelerated)
        {
            var v23s = Vector128.Create<byte>(0x23);
            var bytes1 = src.ReadVector128() - v23s;
            var bytes2 = src.Add(16).ReadVector128() - v23s;
            Vector128<ulong> b1 = Decode(bytes1);
            Vector128<ulong> b2 = Decode(bytes2);
            MergeAndWriteULongs(b1[0], b1[1], b2[0], b2[1], ref dst);
        }
        else if (!DisableV64 && Vector64.IsHardwareAccelerated)
        {
            var v23s = Vector64.Create<byte>(0x23);
            var bytes1 = src.ReadVector64() - v23s;
            var bytes2 = src.Add(8).ReadVector64() - v23s;
            var bytes3 = src.Add(16).ReadVector64() - v23s;
            var bytes4 = src.Add(24).ReadVector64() - v23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            ulong b3 = Decode(bytes3);
            ulong b4 = Decode(bytes4);
            MergeAndWriteULongs(b1, b2, b3, b4, ref dst);
        }
        else
        {
            var bytes1 = src.ReadULong() - UL23s;
            var bytes2 = src.Add(8).ReadULong() - UL23s;
            var bytes3 = src.Add(16).ReadULong() - UL23s;
            var bytes4 = src.Add(24).ReadULong() - UL23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            ulong b3 = Decode(bytes3);
            ulong b4 = Decode(bytes4);
            MergeAndWriteULongs(b1, b2, b3, b4, ref dst);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Decode16Bytes(ref byte src, ref byte dst)
    {
        if (!DisableV128 && Vector128.IsHardwareAccelerated)
        {
            var bytes = src.ReadVector128() - Vector128.Create<byte>(0x23);
            Vector128<ulong> b = Decode(bytes);
            MergeAndWriteULongs(b[0], b[1], ref dst);
        }
        else if (!DisableV64 && Vector64.IsHardwareAccelerated)
        {
            var v23s = Vector64.Create<byte>(0x23);
            var bytes1 = src.ReadVector64() - v23s;
            var bytes2 = src.Add(8).ReadVector64() - v23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            MergeAndWriteULongs(b1, b2, ref dst);
        }
        else
        {
            var bytes1 = src.ReadULong() - UL23s;
            var bytes2 = src.Add(8).ReadULong() - UL23s;
            ulong b1 = Decode(bytes1);
            ulong b2 = Decode(bytes2);
            MergeAndWriteULongs(b1, b2, ref dst);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DecodeBytes(ref byte src, ref byte dst,
                                    [ConstantExpected(Min = 2, Max = 15)] uint readLength,
                                    [ConstantExpected(Min = 1, Max = 12)] uint writeLength)
    {
        Debug.Assert(readLength >= 2 & readLength <= 15);

        if (readLength > 8)
        {
            var bytes1 = src.ReadULong() - UL23s;
            var bytes2 = (src.Add(readLength - 8).ReadULong() - UL23s) >> (int)(8 * (16 - readLength));
            if (!DisableV128 && Vector128.IsHardwareAccelerated)
            {
                Vector128<ulong> b = Decode(Vector128.Create(bytes1, bytes2).AsByte());
                MergeAndWriteULongs(b[0], b[1], ref dst, writeLength);
            }
            else if (!DisableV64 && Vector64.IsHardwareAccelerated)
            {
                ulong b1 = Decode(Vector64.Create(bytes1).AsByte());
                ulong b2 = Decode(Vector64.Create(bytes2).AsByte());
                MergeAndWriteULongs(b1, b2, ref dst, writeLength);
            }
            else
            {
                ulong b1 = Decode(bytes1);
                ulong b2 = Decode(bytes2);
                MergeAndWriteULongs(b1, b2, ref dst, writeLength);
            }
        }
        else if (readLength == 8)
        {
            var bytes = src.ReadULong() - UL23s;
            if (!DisableV64 && Vector64.IsHardwareAccelerated)
            {
                ulong b = Decode(Vector64.Create(bytes).AsByte());
                MergeAndWriteULong(b, ref dst, writeLength);
            }
            else if (!DisableV128 && Vector128.IsHardwareAccelerated)
            {
                Vector128<ulong> b = Decode(Vector128.CreateScalarUnsafe(bytes).AsByte());
                MergeAndWriteULong(b[0], ref dst, writeLength);
            }
            else
            {
                ulong b = Decode(bytes);
                MergeAndWriteULong(b, ref dst, writeLength);
            }
        }
        else if (readLength > 4)
        {
            uint bytes1 = src.ReadUInt() - UI23s;
            uint bytes2 = (src.Add(readLength - 4).ReadUInt() - UI23s) >> (int)(8 * (8 - readLength));
            if (!DisableV64 && Vector64.IsHardwareAccelerated)
            {
                ulong b = Decode(Vector64.Create(bytes1, bytes2).AsByte());
                MergeAndWriteULong(b, ref dst, writeLength);
            }
            if (!DisableV128 && Vector128.IsHardwareAccelerated)
            {
                ulong bytes = ((ulong)bytes1) | ((ulong)bytes2 << 32);
                Vector128<ulong> b = Decode(Vector128.CreateScalarUnsafe(bytes).AsByte());
                MergeAndWriteULong(b[0], ref dst, writeLength);
            }
            else
            {
                ulong bytes = ((ulong)bytes1) | ((ulong)bytes2 << 32);
                ulong b = Decode(bytes);
                MergeAndWriteULong(b, ref dst, writeLength);
            }
        }
        else if (readLength == 4)
        {
            uint bytes = src.ReadUInt() - UI23s;
            if (!DisableV64 && Vector64.IsHardwareAccelerated)
            {
                uint b = (uint)Decode(Vector64.CreateScalarUnsafe(bytes).AsByte());
                MergeAndWriteUInt(b, ref dst, writeLength);
            }
            if (!DisableV128 && Vector128.IsHardwareAccelerated)
            {
                Vector128<uint> b = Decode(Vector128.CreateScalarUnsafe(bytes).AsByte()).AsUInt32();
                MergeAndWriteUInt(b[0], ref dst, writeLength);
            }
            else
            {
                uint b = Decode(bytes);
                MergeAndWriteUInt(b, ref dst, writeLength);
            }
        }
        else if (readLength == 3)
        {
            uint bytes = (src.ReadUShort() | ((uint)src.Add(2).ReadByte() << 16)) - 0x232323;
            if (!DisableV64 && Vector64.IsHardwareAccelerated)
            {
                uint b = (uint)Decode(Vector64.CreateScalarUnsafe(bytes).AsByte());
                MergeAndWriteUInt(b, ref dst, writeLength);
            }
            if (!DisableV128 && Vector128.IsHardwareAccelerated)
            {
                Vector128<uint> b = Decode(Vector128.CreateScalarUnsafe(bytes).AsByte()).AsUInt32();
                MergeAndWriteUInt(b[0], ref dst, writeLength);
            }
            else
            {
                uint b = Decode(bytes);
                MergeAndWriteUInt(b, ref dst, writeLength);
            }
        }
        else if (readLength == 2)
        {
            ushort bytes = (ushort)(src.ReadUShort() - 0x2323);
            ushort b = Decode(bytes);
            MergeAndWriteUShort(b, ref dst, writeLength);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector512<ulong> Decode(Vector512<byte> bytes)
    {
        bytes = Vector512.ConditionalSelect(
                            Vector512.Equals(bytes, Vector512.Create<byte>(91)),
                            Vector512.Create<byte>(57),
                            bytes);
        var b = bytes.AsUInt16();
        b -= (b >>> 8) * (256 - 91);
        return b.AsUInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ulong> Decode(Vector256<byte> bytes)
    {
        bytes = Vector256.ConditionalSelect(
                            Vector256.Equals(bytes, Vector256.Create<byte>(91)),
                            Vector256.Create<byte>(57),
                            bytes);
        var b = bytes.AsUInt16();
        b -= (b >>> 8) * (256 - 91);
        return b.AsUInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ulong> Decode(Vector128<byte> bytes)
    {
        bytes = Vector128.ConditionalSelect(
                            Vector128.Equals(bytes, Vector128.Create<byte>(91)),
                            Vector128.Create<byte>(57),
                            bytes);
        var b = bytes.AsUInt16();
        b -= (b >>> 8) * (256 - 91);
        return b.AsUInt64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Decode(Vector64<byte> bytes)
    {
        bytes = Vector64.ConditionalSelect(
                            Vector64.Equals(bytes, Vector64.Create<byte>(91)),
                            Vector64.Create<byte>(57),
                            bytes);
        var b = bytes.AsUInt16();
        b -= (b >>> 8) * (256 - 91);
        return b.AsUInt64().ToScalar();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Decode(ulong bytes)
    {
        return Decode((uint)bytes) | ((ulong)Decode((uint)(bytes >> 32)) << 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Decode(uint bytes)
    {
        return Decode((ushort)bytes) | ((uint)Decode((ushort)(bytes >> 16)) << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort Decode(ushort bytes)
    {
        var r = bytes & 0xFFU;
        var q = (uint)bytes >> 8;
        r = (r == 91) ? 57 : r;
        q = (q == 91) ? 57 : q;
        return (ushort)(r + q * 91);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteULongs(ulong b1, ulong b2, ulong b3, ulong b4,
                                            ulong b5, ulong b6, ulong b7, ulong b8,
                                            ref byte dst)
    {
        MergeAndWriteULongs(b1, b2, b3, b4, ref dst);
        MergeAndWriteULongs(b5, b6, b7, b8, ref dst.Add(26));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteULongs(ulong b1, ulong b2, ulong b3, ulong b4, ref byte dst)
    {
        MergeAndWriteULongs(b1, b2, ref dst);
        MergeAndWriteULongs(b3, b4, ref dst.Add(13));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteULongs(ulong b1, ulong b2, ref byte dst,
                                      [ConstantExpected(Max = 13)] uint length = 13)
    {
        b1 = b1.Merge();
        b2 = b2.Merge();
        b1 |= b2 << (64 - 12);
        if (length >= 8)
        {
            dst.WriteULong(b1);
            if (length > 8)
            {
                b2 >>>= 12;
                dst.Add(8).Write(b2, length - 8);
            }
        }
        else
        {
            dst.Write(b1, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteULong(ulong b1, ref byte dst, [ConstantExpected(Min = 4, Max = 6)] uint length)
    {
        b1 = b1.Merge();
        dst.Write(b1, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteUInt(uint b1, ref byte dst, [ConstantExpected(Min = 2, Max = 3)] uint length)
    {
        b1 = b1.Merge();
        dst.Write(b1, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MergeAndWriteUShort(ushort b1, ref byte dst, [ConstantExpected(Min = 0, Max = 1)] uint length)
    {
        dst.Write(b1, length);
    }
}
