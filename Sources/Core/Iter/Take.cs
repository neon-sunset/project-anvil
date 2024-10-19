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
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(take.iter, Math.Min(take.count, count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Take<T, U> Take<T, U>(this T iter, nuint count, TArgs<U> _ = default)
    where T: Iter<U>, allows ref struct
    where U: allows ref struct => new(iter, count);
}

public ref struct Take<T, U>(T iter, nuint count): Iter<U>
where T: Iter<U>, allows ref struct
where U: allows ref struct {
    internal T iter = iter;
    internal nuint count = count;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint c ? Math.Min(c, count) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        if (count > 0 && iter.Next(out item)) {
            count--;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
