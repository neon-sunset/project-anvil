using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Prepend<T, U> Prepend<T, U>(this T iter, U value)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(iter, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Prepend<SpanIter<T>, T> Prepend<T>(this Span<T> span, T value)
        => new(new(span), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Prepend<SpanIter<T>, T> Prepend<T>(this ReadOnlySpan<T> span, T value)
        => new(new(span), value);
}

public ref struct Prepend<T, U>(T iter, U value): Iter<U>
where T: Iter<U>, allows ref struct
where U: allows ref struct {
    T iter = iter;
    U value = value;
    bool prepended;

    public nuint? Count => iter.Count is nuint count ? count + 1 : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        if (prepended) {
            return iter.Next(out item);
        }
        prepended = true;
        item = value;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose() => iter.Dispose();
}