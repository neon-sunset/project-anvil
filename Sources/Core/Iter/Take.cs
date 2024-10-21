using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Take<T>(this Span<T> span, nuint count)
        => Take((ReadOnlySpan<T>)span, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Take<T>(this ReadOnlySpan<T> span, nuint count) {
        var length = (int)(uint)Math.Min((uint)span.Length, count);
        return new(span[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Take<T, U> Take<T, U>(this Take<T, U> take, nuint count)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct => new(take.iter, Math.Min(take.take, count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Take<T, U> Take<T, U>(this T iter, nuint count, TArgs<U> _ = default)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct => new(iter, count);
}

public ref struct Take<T, U>(T iter, nuint count): Iterator<U>
where T: Iterator<U>, allows ref struct
where U: allows ref struct {
    internal T iter = iter;
    internal nuint take = count;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint c ? Math.Min(c, take) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        if (take > 0 && iter.Next(out item)) {
            take--;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        if (count > 0) {
            if (count >= take) {
                count = count - take + iter.AdvanceBy(take);
                take = 0;
            } else {
                take -= count;
                count = iter.AdvanceBy(count);
            }
        }
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
