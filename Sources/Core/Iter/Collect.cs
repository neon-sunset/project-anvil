using System.Allocators;
using System.Generics;
using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public static U Collect<T, U, V>(this T iter, TArgs<U, V> _ = default)
    //where T: Iter<V>, allows ref struct
    //where U: ConvIter<U, V>, allows ref struct
    //where V: allows ref struct => U.From(iter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, Global> Collect<T, U>(this U iter, TArgs<T> _ = default)
    where T : unmanaged
    where U : Iterator<T>, allows ref struct => NVec<T, Global>.Collect(iter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, A> Collect<T, A, U>(this U iter, A _, TArgs<T> __ = default)
    where T: unmanaged
    where A: NativeAllocator
    where U: Iterator<T>, allows ref struct => NVec<T, A>.Collect(iter);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, Global> Collect<T, U, V>(this U iter, Func<V, T> func)
    where T : unmanaged
    where U : Iterator<V>, allows ref struct => NVec<T, Global>.Collect(iter.Select(func));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, A> Collect<T, A, U, V>(this U iter, Func<V, T> func, A _)
    where T: unmanaged
    where A: NativeAllocator
    where U: Iterator<V>, allows ref struct => NVec<T, A>.Collect(iter.Select(func));
}
