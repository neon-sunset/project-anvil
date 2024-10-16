using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

static class Memory {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(ref T ptr, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref ptr, (int)(uint)size)
                .Clear();
            ptr = ref Unsafe.Add(ref ptr, size);
            count -= size;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy<T>(ref T src, ref T dst, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref src, (int)(uint)size)
                .CopyTo(MemoryMarshal.CreateSpan(ref dst, (int)(uint)size));
            src = ref Unsafe.Add(ref src, size);
            dst = ref Unsafe.Add(ref dst, size);
            count -= size;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill<T>(ref T ptr, T value, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref ptr, (int)(uint)size)
                .Fill(value);
            ptr = ref Unsafe.Add(ref ptr, size);
            count -= size;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint? IndexOf<T>(ref T ptr, nuint length, T value)
    where T: IEquatable<T> {
        var offset = (nuint)0;
        while (length > 0) {
            var size = Math.Min(length, int.MaxValue);
            var span = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.Add(ref ptr, offset), (int)(uint)size);
            var index = span.IndexOf(value);
            if (index > -1) return offset + (uint)index;

            offset += size;
            length -= size;
        }
        return null;
    }
}

public ref struct RefEnumerator<T>: IEnumerator<T> {
    readonly ref T end;
    ref T current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefEnumerator(ReadOnlySpan<T> span) {
        current = ref MemoryMarshal.GetReference(span);
        end = ref Unsafe.Add(ref current, span.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefEnumerator(ref T start, nuint length) {
        current = ref start;
        end = ref Unsafe.Add(ref start, length);
    }

    public T Current { readonly get; private set; } = default!;

    readonly object IEnumerator.Current => Current!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() {
        ref var curr = ref current;
        if (Unsafe.IsAddressLessThan(ref curr, ref end)) {
            Current = curr;
            current = ref Unsafe.Add(ref curr, 1);
            return true;
        }
        return false;
    }

    readonly void IDisposable.Dispose() { }
    readonly void IEnumerator.Reset() => throw new NotSupportedException();
}

public unsafe struct PtrEnumerator<T>: IEnumerator<T>
where T: unmanaged {
    readonly T* end;
    T* current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PtrEnumerator(T* start, nuint length) {
        current = start;
        end = start + length;
    }

    public T Current { readonly get; private set; }
    readonly object IEnumerator.Current => Current!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() {
        var curr = current;
        if (curr < end) {
            Current = *curr;
            current = curr + 1;
            return true;
        }
        return false;
    }

    readonly void IDisposable.Dispose() { }
    readonly void IEnumerator.Reset() => throw new NotSupportedException();
}
