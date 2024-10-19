using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Append<T, U> Append<T, U>(this T iter, U value)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(iter, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Append<RefIter<T>, T> Append<T>(this Span<T> span, T value)
        => new(new(span), value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Append<RefIter<T>, T> Append<T>(this ReadOnlySpan<T> span, T value)
        => new(new(span), value);
}

public ref struct Append<T, U>(T iter, U value): Iter<U>
where T: Iter<U>, allows ref struct
where U: allows ref struct {
    T iter = iter;
    U value = value;
    bool done;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint count ? count + 1 : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        if (!done) {
            if (iter.Next(out item)) {
                return true;
            }
            item = value;
            done = true;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose() => iter.Dispose();
}
