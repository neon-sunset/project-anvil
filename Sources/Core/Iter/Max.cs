using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Max<T>(this ReadOnlySpan<T> span)
    where T: IComparable<T> {
        ArgumentOutOfRangeException.ThrowIfZero(span.Length);

        if (typeof(T) == typeof(byte))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, byte>());
        if (typeof(T) == typeof(sbyte))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, sbyte>());
        if (typeof(T) == typeof(short))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, short>());
        if (typeof(T) == typeof(ushort))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, ushort>());
        if (typeof(T) == typeof(int))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, int>());
        if (typeof(T) == typeof(uint))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, uint>());
        if (typeof(T) == typeof(long))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, long>());
        if (typeof(T) == typeof(ulong))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, ulong>());
        if (typeof(T) == typeof(nuint))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, nuint>());
        if (typeof(T) == typeof(nint))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, nint>());
        if (typeof(T) == typeof(Half))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, Half>());
        if (typeof(T) == typeof(float))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, float>());
        if (typeof(T) == typeof(double))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, double>());
        if (typeof(T) == typeof(NFloat))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, NFloat>());
        if (typeof(T) == typeof(decimal))
            return (T)(object)TensorPrimitives.Max(span.UncheckedCast<T, decimal>());

        throw new NotSupportedException();
    }
}
