using System.Runtime.CompilerServices;
using Anvil.Core;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Nth<T, U>(this T iter, nuint n, out U item)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct {
        while (iter.Next(out item)) {
            if (n == 0) {
                return true;
            }
            n--;
        }
        Throw.IndexOutOfRange<int>();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Nth<T, U>(this Slice<U> values, nuint n, out U item)
        => Nth(values.Iter(), n, out item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Nth<T, U>(this Span<U> values, nuint n, out U item)
        => Nth(values.Iter(), n, out item);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Nth<T, U>(this ReadOnlySpan<U> values, nuint n, out U item)
        => Nth(values.Iter(), n, out item);
}
