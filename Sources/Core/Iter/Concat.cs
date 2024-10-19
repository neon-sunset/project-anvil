using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Concat<T, U, V> Concat<T, U, V>(this T lhs, U rhs, TArgs<V> _ = default)
    where T: Iter<V>, allows ref struct
    where U: Iter<V>, allows ref struct
    where V: allows ref struct => new(lhs, rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Concat<T, RefIter<U>, U> Concat<T, U>(this T lhs, ReadOnlySpan<U> rhs)
    where T: Iter<U>, allows ref struct => new(lhs, new(rhs));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Concat<RefIter<T>, U, T> Concat<T, U>(this Span<T> lhs, U rhs)
    where U: Iter<T>, allows ref struct => new(new(lhs), rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Concat<RefIter<T>, U, T> Concat<T, U>(this ReadOnlySpan<T> lhs, U rhs)
    where U: Iter<T>, allows ref struct => new(new(lhs), rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Concat<RefIter<T>, RefIter<T>, T> Concat<T>(
        this ReadOnlySpan<T> lhs, ReadOnlySpan<T> rhs
    ) => new(new(lhs), new(rhs));
}

public ref struct Concat<T, U, V>(T lhs, U rhs): Iter<V>
where T: Iter<V>, allows ref struct
where U: Iter<V>, allows ref struct
where V: allows ref struct {
    T lhs = lhs;
    U rhs = rhs;
    bool right;

    public nuint? Count {
        // TODO: Handle overflow?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => lhs.Count is nuint lcount
            && rhs.Count is nuint rcount ? lcount + rcount : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out V item) {
        if (!right) {
            if (lhs.Next(out item)) {
                return true;
            }
            right = true;
        }
        return rhs.Next(out item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose() {
        // TODO: Ensure second dispose is called even if first fails?
        lhs.Dispose();
        rhs.Dispose();
    }
}
