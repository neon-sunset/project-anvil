using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cast<T, U, V> Cast<T, U, V>(this T iter, TArgs<U, V> _ = default!)
    where T: Iterator<U>, allows ref struct => new(iter);
}

public ref struct Cast<T, U, V>(T iter): Iterator<V>
where T: Iterator<U>, allows ref struct {
    T iter = iter;

    public nuint? Count => iter.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out V item) {
        if (iter.Next(out var from)) {
            // Doesn't fucking work because apparently
            // we don't need ICastOperators<T, U>
            item = (V)(object)from!;
            return true;
        }
        item = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) => iter.AdvanceBy(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose() => iter.Dispose();
}
