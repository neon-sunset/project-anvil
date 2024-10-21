using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Skip<T>(this Span<T> span, nuint count)
        => Skip((ReadOnlySpan<T>)span, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Skip<T>(this ReadOnlySpan<T> span, nuint count)
        => new(span[(int)Math.Max((uint)span.Length, count)..]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skip<T, U> Skip<T, U>(this Skip<T, U> skip, nuint count)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct => new(skip.iter, checked(skip.skip + count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Skip<T, U> Skip<T, U>(this T iter, nuint count, TArgs<U> _ = default)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct => new(iter, count);
}

public ref struct Skip<T, U>(T iter, nuint count): Iterator<U>
where T: Iterator<U>, allows ref struct
where U: allows ref struct {
    internal T iter = iter;
    internal nuint skip = count;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint c && c > skip ? c - skip : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
    next:
        var next = iter.Next(out item);
        if (next && skip > 0) {
            skip--;
            goto next;
        }
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        if (count > 0) {
            var rem = iter.AdvanceBy(count + skip);
            // By this point, we consider the iterator skipped.
            count = rem > skip ? rem - skip : 0;
            skip = 0;
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
