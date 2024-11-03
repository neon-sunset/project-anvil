using System.Runtime.CompilerServices;

namespace System;

public unsafe partial struct NVec<T, A> {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Slice<T>(NVec<T, A> source) => new(source.items, source.count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MutSlice<T>(NVec<T, A> source) => new(source.items, source.count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(NVec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(NVec<T, A> source) => source.AsSpan();
}
