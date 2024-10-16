using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public readonly ref struct Slice<T>:
    As<ReadOnlySpan<T>>
{
    readonly ref T ptr;
    readonly nuint length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Slice(ReadOnlySpan<T> values) {
        ptr = ref MemoryMarshal.GetReference(values);
        length = (uint)values.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Slice(void* ptr, nuint length) {
        this.ptr = ref Unsafe.AsRef<T>(ptr);
        this.length = length;
    }

    public ref readonly T this[nuint index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ArgumentOutOfRangeException
                .ThrowIfGreaterThanOrEqual(index, length);
            return ref Unsafe.Add(ref ptr, index);
        }
    }

    public nuint Length {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }

    ReadOnlySpan<T> As<ReadOnlySpan<T>>.As()
        => MemoryMarshal.CreateReadOnlySpan(ref ptr, checked((int)length));
}

public readonly ref struct MutSlice<T>:
    As<Span<T>>
{
    readonly ref T ptr;
    readonly nuint length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MutSlice(Span<T> values) {
        ptr = ref MemoryMarshal.GetReference(values);
        length = (uint)values.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe MutSlice(void* ptr, nuint length) {
        this.ptr = ref Unsafe.AsRef<T>(ptr);
        this.length = length;
    }

    public ref T this[nuint index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            ArgumentOutOfRangeException
                .ThrowIfGreaterThanOrEqual(index, length);
            return ref Unsafe.Add(ref ptr, index);
        }
    }

    public nuint Length {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => length;
    }

    Span<T> As<Span<T>>.As()
        => MemoryMarshal.CreateSpan(ref ptr, checked((int)length));
}
