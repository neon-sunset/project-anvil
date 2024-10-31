using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Fold<T, U, V>(this T iter, U init, Func<U, V, U> f)
    where T: Iterator<V>, allows ref struct
    where U: allows ref struct {
        using (iter) {
            while (iter.Next(out var item)) {
                init = f(init, item);
            }

            return init;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Fold<T, U, V>(this Slice<V> values, U init, Func<U, V, U> f)
        => Fold(values.Iter(), init, f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Fold<T, U, V>(this Span<V> values, U init, Func<U, V, U> f)
        => Fold(values.Iter(), init, f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Fold<T, U, V>(this ReadOnlySpan<V> values, U init, Func<U, V, U> f)
        => Fold(values.Iter(), init, f);
}
