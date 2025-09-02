using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Base91s;

internal static partial class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector64<uint> WidenUShorts(this uint v)
    {
        return Vector64.WidenLower(Vector64.CreateScalarUnsafe(v).AsUInt16());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<uint> WidenUShorts(this ulong v)
    {
        return Vector128.WidenLower(Vector128.CreateScalarUnsafe(v).AsUInt16());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<uint> Widen(this Vector64<ushort> v)
    {
        if (AdvSimd.IsSupported)
            AdvSimd.ZeroExtendWideningLower(v);
        return Vector128.WidenLower(v.ToVector128Unsafe());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<uint> Widen(this Vector128<ushort> v)
    {
        if (Avx2.IsSupported)
            return Avx2.ConvertToVector256Int32(v).AsUInt32();
        return Vector256.WidenLower(v.ToVector256Unsafe());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector512<uint> Widen(this Vector256<ushort> v)
    {
        if (Avx512F.IsSupported)
            return Avx512F.ConvertToVector512Int32(v).AsUInt32();
        return Vector512.WidenLower(v.ToVector512Unsafe());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint NarrowToUInt(this Vector64<uint> v)
    {
        return Vector64.Narrow(v, v).AsUInt32().ToScalar();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong NarrowToULong(this Vector128<uint> v)
    {
        if (Sse41.IsSupported)
            return Sse41.PackUnsignedSaturate(v.AsInt32(), v.AsInt32()).AsUInt64().ToScalar();
        if (AdvSimd.IsSupported)
            return AdvSimd.ExtractNarrowingLower(v).AsUInt64().ToScalar();
        return Vector64.Narrow(v.GetLower(), v.GetUpper()).AsUInt64().ToScalar();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector64<ushort> Narrow(this Vector128<uint> v)
    {
        if (Sse41.IsSupported)
            return Sse41.PackUnsignedSaturate(v.AsInt32(), v.AsInt32()).GetLower();
        if (AdvSimd.IsSupported)
            return AdvSimd.ExtractNarrowingLower(v);
        return Vector64.Narrow(v.GetLower(), v.GetUpper());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<ushort> Narrow(this Vector256<uint> v)
    {
        if (Avx512F.VL.IsSupported)
            return Avx512F.VL.ConvertToVector128UInt16(v);
        if (Sse41.IsSupported)
            return Sse41.PackUnsignedSaturate(v.GetLower().AsInt32(), v.GetUpper().AsInt32());
        return Vector128.Narrow(v.GetLower(), v.GetUpper());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<ushort> Narrow(this Vector512<uint> v)
    {
        if (Avx512F.VL.IsSupported)
            return Avx512F.ConvertToVector256UInt16(v);
        if (Avx2.IsSupported)
            return Avx2.PackUnsignedSaturate(v.GetLower().AsInt32(), v.GetUpper().AsInt32());
        return Vector256.Narrow(v.GetLower(), v.GetUpper());
    }
}
