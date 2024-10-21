using System.Allocators;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<RefIter<T>, T, V> Select<T, V>(this Span<T> span, Func<T, V> func)
    where V: allows ref struct => new(new(span), func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<RefIter<T>, T, V> Select<T, V>(this ReadOnlySpan<T> span, Func<T, V> func)
    where V: allows ref struct => new(new(span), func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<RefIter<T>, T, V> Select<T, V, A>(this NVec<T, A> vec, Func<T, V> func)
    where T: unmanaged
    where V: allows ref struct
    where A: NativeAllocator => new(new(vec), func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<T, U, V> Select<T, U, V>(this T iter, Func<U, V> func)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct
    where V: allows ref struct => new(iter, func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<T, F, U, V> Select<T, F, U, V>(this T iter, F func)
    where T: Iterator<U>, allows ref struct
    where F: Fn<U, V>, allows ref struct
    where U: allows ref struct
    where V: allows ref struct => new(iter, func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<T, U, V> Select<T, U, X, V>(this Select<T, U, X> iter, Func<X, V> f2)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct
    where X: allows ref struct
    where V: allows ref struct {
        var f1 = iter.func;
        return new(iter.iter, x => f2(f1(x)));
    }

}

public ref struct Select<T, U, V>(T iter, Func<U, V> func): Iterator<V>
where T: Iterator<U>, allows ref struct
where U: allows ref struct
where V: allows ref struct {
    internal T iter = iter;
    internal Func<U, V> func = func;

    public nuint? Count => iter.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out V item) {
        if (iter.Next(out var value)) {
            item = func(value);
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) => iter.AdvanceBy(count);

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Select<T, U, V>, V> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}

public ref struct Select<T, F, U, V>(T iter, F func): Iterator<V>
where T: Iterator<U>, allows ref struct
where F: Fn<U, V>, allows ref struct
where U: allows ref struct
where V: allows ref struct {
    internal T iter = iter;
    internal F func = func;

    public nuint? Count => iter.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out V item) {
        if (iter.Next(out var value)) {
            item = func.Invoke(value);
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        while (count > 0 && iter.Next(out _))
            count--;
        return count;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Select<T, F, U, V>, V> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
