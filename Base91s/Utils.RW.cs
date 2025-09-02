using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Base91s;

internal static partial class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector512<byte> ReadVector512(this ref byte src)
    {
        if (BitConverter.IsLittleEndian)
            return Unsafe.ReadUnaligned<Vector512<byte>>(ref src);
        return Vector512.Create(src.ReadULong(),
                                src.Add(8).ReadULong(),
                                src.Add(16).ReadULong(),
                                src.Add(24).ReadULong(),
                                src.Add(32).ReadULong(),
                                src.Add(40).ReadULong(),
                                src.Add(48).ReadULong(),
                                src.Add(56).ReadULong()
                               ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<byte> ReadVector256(this ref byte src)
    {
        if (BitConverter.IsLittleEndian)
            return Unsafe.ReadUnaligned<Vector256<byte>>(ref src);
        return Vector256.Create(src.ReadULong(),
                                src.Add(8).ReadULong(),
                                src.Add(16).ReadULong(),
                                src.Add(24).ReadULong()
                               ).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> ReadVector128(this ref byte src)
    {
        if (BitConverter.IsLittleEndian)
            return Unsafe.ReadUnaligned<Vector128<byte>>(ref src);
        return Vector128.Create(src.ReadULong(), src.Add(8).ReadULong()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector64<byte> ReadVector64(this ref byte src)
    {
        if (BitConverter.IsLittleEndian)
            return Unsafe.ReadUnaligned<Vector64<byte>>(ref src);
        return Vector64.Create(src.ReadULong()).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReadULong(this ref byte src)
    {
        var value = Unsafe.ReadUnaligned<ulong>(ref src);
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint ReadUInt(this ref byte src)
    {
        var value = Unsafe.ReadUnaligned<uint>(ref src);
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ushort ReadUShort(this ref byte src)
    {
        var value = Unsafe.ReadUnaligned<ushort>(ref src);
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ReadByte(this ref byte src)
    {
        return src;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(this ref byte dst, Vector512<T> value, [ConstantExpected(Max = 64)] uint length)
    {
        dst.Write(value.AsUInt64(), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, Vector512<ulong> value, [ConstantExpected(Max = 64)] uint length)
    {
        Debug.Assert(length <= 64);

        if (length == 64)
        {
            dst.WriteVector512(value);
        }
        else if (length >= 32)
        {
            dst.WriteVector256(value.GetLower());
            if (length > 32)
                dst.Add(32).Write(value.GetUpper(), length - 32);
        }
        else
        {
            dst.Write(value.GetLower(), length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(this ref byte dst, Vector256<T> value, [ConstantExpected(Max = 32)] uint length)
    {
        dst.Write(value.AsUInt64(), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, Vector256<ulong> value, [ConstantExpected(Max = 32)] uint length)
    {
        Debug.Assert(length <= 32);

        if (length == 32)
        {
            dst.WriteVector256(value);
        }
        else if (length >= 16)
        {
            dst.WriteVector128(value.GetLower());
            if (length > 16)
                dst.Add(16).Write(value.GetUpper(), length - 16);
        }
        else
        {
            dst.Write(value.GetLower(), length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(this ref byte dst, Vector128<T> value, [ConstantExpected(Max = 16)] uint length)
    {
        dst.Write(value.AsUInt64(), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, Vector128<ulong> value, [ConstantExpected(Max = 16)] uint length)
    {
        Debug.Assert(length <= 16);

        if (length == 16)
        {
            dst.WriteVector128(value);
        }
        else if (length >= 8)
        {
            dst.WriteULong(value[0]);
            if (length > 8)
                dst.Add(8).Write(value[1], length - 8);
        }
        else
        {
            dst.Write(value[0], length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write<T>(this ref byte dst, Vector64<T> value, [ConstantExpected(Max = 8)] uint length)
    {
        dst.Write(value.AsUInt64(), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, Vector64<ulong> value, [ConstantExpected(Max = 8)] uint length)
    {
        Debug.Assert(length <= 8);
        dst.Write(value.ToScalar(), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, ulong value, [ConstantExpected(Max = 8)] uint length)
    {
        Debug.Assert(length <= 8);

        if (length == 8)
        {
            dst.WriteULong(value);
        }
        else if (length >= 4)
        {
            dst.WriteUInt((uint)value);
            if (length > 4)
                dst.Add(4).Write((uint)(value >> 32), length - 4);
        }
        else
        {
            dst.Write((uint)value, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, uint value, [ConstantExpected(Max = 4)] uint length)
    {
        Debug.Assert(length <= 4);

        if (length == 4)
        {
            dst.WriteUInt(value);
        }
        else if (length >= 2)
        {
            dst.WriteUShort((ushort)value);
            if (length == 3)
                dst.Add(2).WriteByte((byte)(value >> 16));
        }
        else if (length == 1)
        {
            dst.WriteByte((byte)value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Write(this ref byte dst, ushort value, [ConstantExpected(Max = 2)] uint length)
    {
        Debug.Assert(length <= 2);

        if (length == 2)
            dst.WriteUShort(value);
        else if (length == 1)
            dst.WriteByte((byte)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteULong(this ref byte dst, ulong value)
    {
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector512<T>(this ref byte dst, Vector512<T> value)
    {
        dst.WriteVector512(value.AsUInt64());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector512(this ref byte dst, Vector512<ulong> value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            dst.WriteULong(value[0]);
            dst.Add(8).WriteULong(value[1]);
            dst.Add(16).WriteULong(value[2]);
            dst.Add(24).WriteULong(value[3]);
            dst.Add(32).WriteULong(value[4]);
            dst.Add(40).WriteULong(value[5]);
            dst.Add(48).WriteULong(value[6]);
            dst.Add(56).WriteULong(value[7]);
            return;
        }
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector256<T>(this ref byte dst, Vector256<T> value)
    {
        dst.WriteVector256(value.AsUInt64());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector256(this ref byte dst, Vector256<ulong> value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            dst.WriteULong(value[0]);
            dst.Add(8).WriteULong(value[1]);
            dst.Add(16).WriteULong(value[2]);
            dst.Add(24).WriteULong(value[3]);
            return;
        }
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector128<T>(this ref byte dst, Vector128<T> value)
    {
        dst.WriteVector128(value.AsUInt64());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteVector128(this ref byte dst, Vector128<ulong> value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            dst.WriteULong(value[0]);
            dst.Add(8).WriteULong(value[1]);
            return;
        }
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUInt(this ref byte dst, uint value)
    {
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteUShort(this ref byte dst, ushort value)
    {
        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref dst, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteByte(this ref byte dst, byte value)
    {
        Unsafe.WriteUnaligned(ref dst, value);
    }
}
