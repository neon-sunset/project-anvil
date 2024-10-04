using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Anvil.Core;

interface Arr<T>:
    Into<ReadOnlySpan<T>> {
}

public ref struct ScopedEnumerator<T>: IEnumerator<T> {
    readonly ref T end;
    ref T current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScopedEnumerator(ReadOnlySpan<T> span) {
        current = ref MemoryMarshal.GetReference(span);
        end = ref Unsafe.Add(ref current, span.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ScopedEnumerator(ref T start, nuint length) {
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

public unsafe struct UnmanagedEnumerator<T>: IEnumerator<T>
where T: unmanaged {
    readonly T* end;
    T* current;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UnmanagedEnumerator(T* start, nuint length) {
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
