using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Skip<T>(this Span<T> span, nuint count)
        => Skip((ReadOnlySpan<T>)span, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Skip<T>(this ReadOnlySpan<T> span, nuint count) {
        return new(span[(int)Math.Max((uint)span.Length, count)..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skip<T, U> Skip<T, U>(this Skip<T, U> skip, nuint count)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(skip.iter, skip.count + count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skip<T, U> Skip<T, U>(this T iter, nuint count, TArgs<U> _ = default)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(iter, count);
}

public ref struct Skip<T, U>(T iter, nuint count)
where T: Iter<U>, allows ref struct
where U: allows ref struct {
    internal T iter = iter;
    internal nuint count = count;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint c && c > count ? c - count : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
    skip:
        var next = iter.Next(out item);
        if (next && count > 0) {
            count--;
            goto skip;
        }
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
