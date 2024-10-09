using System.Allocators;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<T, U, V> Select<T, U, V>(this T iter, Func<U, V> func)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct
    where V: allows ref struct => new(iter, func);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Select<T, U, V> Select<T, U, X, V>(this Select<T, U, X> iter, Func<X, V> f2)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct
    where X: allows ref struct
    where V: allows ref struct {
        var f1 = iter.func;
        return new(iter.iter, x => f2(f1(x)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Where<T, U> Where<T, U>(this T iter, Func<U, bool> predicate)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(iter, predicate);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Where<T, U> Where<T, U>(this Where<T, U> iter, Func<U, bool> p2)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct {
        var p1 = iter.predicate;
        return new(iter.iter, x => p1(x) && p2(x));
    }
}

public ref struct Select<T, U, V>(T iter, Func<U, V> func): Iter<V>
where T: Iter<U>, allows ref struct
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
        item = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Select<T, U, V>, V> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}

public ref struct Where<T, U>(T iter, Func<U, bool> predicate): Iter<U>
where T: Iter<U>, allows ref struct
where U: allows ref struct {
    internal T iter = iter;
    internal Func<U, bool> predicate = predicate;

    public readonly nuint? Count => null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        while (iter.Next(out item)) {
            if (predicate(item)) {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Where<T, U>, U> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
