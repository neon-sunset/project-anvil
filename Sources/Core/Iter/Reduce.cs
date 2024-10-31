using System.Runtime.CompilerServices;
using Anvil.Core;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Reduce<T, U>(this T iter, Func<U, U, U> func)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct {
        using (iter) {
            if (!iter.Next(out var acc)) {
                Throw.EmptySequence();
            }

            while (iter.Next(out var item)) {
                acc = func(acc, item);
            }

            return acc;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Reduce<T>(this Slice<T> values, Func<T, T, T> func)
        => Reduce(values.Iter(), func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Reduce<T>(this Span<T> values, Func<T, T, T> func)
        => Reduce(values.Iter(), func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Reduce<T>(this ReadOnlySpan<T> values, Func<T, T, T> func)
        => Reduce(values.Iter(), func);
}
