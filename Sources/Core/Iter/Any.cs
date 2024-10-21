using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T, U>(this T iter, Func<U, bool> predicate)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct {
        while (iter.Next(out var item)) {
            if (predicate(item)) {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T>(this Span<T> span, Func<T, bool> predicate)
        => Any(span.Iter(), predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        => Any(span.Iter(), predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Any<T>(this Slice<T> slice, Func<T, bool> predicate)
        => Any(slice.Iter(), predicate);
}
