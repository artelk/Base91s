using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Base91s;

internal static partial class Utils
{
    private const bool DisablePdepPext = false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref byte Add(this ref byte src, nuint offset) => ref Unsafe.Add(ref src, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ToByte(this bool value) => Unsafe.BitCast<bool, byte>(value);

    private const ulong Mask64 = 0b0001111111111111_0001111111111111_0001111111111111_0001111111111111UL;
    private const uint Mask32 = 0b0001111111111111_0001111111111111U;
    private const ushort Mask16 = 0b0001111111111111;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Split(this ulong v)
    {
        if (!DisablePdepPext && Bmi2.X64.IsSupported)
            return Bmi2.X64.ParallelBitDeposit(v, Mask64);
        const ulong Mask = Mask16;
        return (v & Mask) |
                ((v & (Mask << 13)) << (16 - 13)) |
                ((v & (Mask << 26)) << (32 - 26)) |
                ((v & (Mask << 39)) << (48 - 39));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Split(this uint v)
    {
        if (!DisablePdepPext && Bmi2.X64.IsSupported)
            return Bmi2.ParallelBitDeposit(v, Mask32);
        const uint Mask = Mask16;
        return (v & Mask) |
            ((v & (Mask << 13)) << (16 - 13));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Merge(this ulong v)
    {
        if (!DisablePdepPext && Bmi2.X64.IsSupported)
            return Bmi2.X64.ParallelBitExtract(v, Mask64);
        const ulong Mask = Mask16;
        return (v & Mask) |
                ((v & (Mask << 16)) >> (16 - 13)) |
                ((v & (Mask << 32)) >> (32 - 26)) |
                ((v & (Mask << 48)) >> (48 - 39));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Merge(this uint v)
    {
        if (!DisablePdepPext && Bmi2.X64.IsSupported)
            return Bmi2.ParallelBitExtract(v, Mask32);
        const uint Mask = Mask16;
        return (v & Mask) |
            ((v & (Mask << 16)) >> (16 - 13));
    }
}
