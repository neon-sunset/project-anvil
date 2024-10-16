using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U Collect<T, U, V>(this T iter, TArgs<U, V> _ = default)
    where T: Iter<V>, allows ref struct
    where U: ConvIter<U, V>, allows ref struct
    where V: allows ref struct => U.From(iter);
}
