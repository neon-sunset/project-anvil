using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All<T, U>(this T iter, Func<U, bool> predicate)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct {
        while (iter.Next(out var item)) {
            if (!predicate(item)) {
                return false;
            }
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All<T>(this Span<T> span, Func<T, bool> predicate)
        => All(span.Iter(), predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool All<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        => All(span.Iter(), predicate);
}
