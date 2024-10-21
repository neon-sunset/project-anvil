using System.ComponentModel;
using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Zip<T, U, V, W> Zip<T, U, V, W>(this T lhs, U rhs, TArgs<V, W> _ = default)
    where T: Iterator<V>, allows ref struct
    where U: Iterator<W>, allows ref struct => new(lhs, rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Zip<T, RefIter<U>, V, U> Zip<T, U, V>(this T lhs, ReadOnlySpan<U> rhs)
    where T: Iterator<V>, allows ref struct => new(lhs, new(rhs));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Zip<RefIter<T>, U, T, V> Zip<T, U, V>(this ReadOnlySpan<T> lhs, U rhs)
    where U: Iterator<V>, allows ref struct => new(new(lhs), rhs);
}

public ref struct Zip<T, U, V, W>(T lhs, U rhs): Iterator<(V, W)>
where T: Iterator<V>, allows ref struct
where U: Iterator<W>, allows ref struct /*
where V: allows ref struct
where W: allows ref struct */ {
    T lhs = lhs;
    U rhs = rhs;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => lhs.Count is nuint lcount
            && rhs.Count is nuint rcount ? Math.Min(lcount, rcount) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out (V, W) item) {
        if (lhs.Next(out var left) &&
            rhs.Next(out var right)
        ) {
            item = (left, right);
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        var lrem = lhs.AdvanceBy(count);
        var rrem = rhs.AdvanceBy(count - lrem);
        return Math.Max(lrem, rrem);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Zip<T, U, V, W>, (V, W)> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        lhs.Dispose();
        rhs.Dispose();
    }
}
