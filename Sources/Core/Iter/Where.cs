using System.Allocators;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Where<RefIter<T>, T> Where<T>(this Span<T> span, Func<T, bool> predicate) {
        return new(new(span), predicate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Where<RefIter<T>, T> Where<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate) {
        return new(new(span), predicate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Where<RefIter<T>, T> Where<T, A>(this NVec<T, A> vec, Func<T, bool> predicate)
    where T: unmanaged
    where A: NativeAllocator => new(new(vec), predicate);

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
