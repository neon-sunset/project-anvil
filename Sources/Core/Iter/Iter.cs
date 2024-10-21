using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Generics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Iter;

public static unsafe partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Iter<T>(
        [UnscopedRef] params Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Iter<T>(
        [UnscopedRef] params ReadOnlySpan<T> span) => new(span);
}

public static partial class OpsExt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<U> Iter<T, U>(this ref T source, TArgs<U> _ = default)
    where T: struct, As<RefIter<U>>, allows ref struct => source.As();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Iter<T>(this Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RefIter<T> Iter<T>(this ReadOnlySpan<T> span) => new(span);
}

public interface Iterator<T>: IDisposable
where T: allows ref struct {
    nuint? Count { get; }
    bool Next(out T item);
    nuint AdvanceBy(nuint count);
}

[SkipLocalsInit]
public ref struct RefIter<T>: Iterator<T> {
    ref T current;
    nuint count;

    public readonly nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefIter(ReadOnlySpan<T> span) {
        current = ref MemoryMarshal.GetReference(span);
        count = (uint)span.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe RefIter(void* start, nuint length) {
        current = ref Unsafe.AsRef<T>(start);
        count = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefIter(ref T start, nuint length) {
        current = ref start;
        count = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out T item) {
        if (count != 0) {
            item = current;
            current = ref Unsafe.Add(ref current, 1);
            count--;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        count = Math.Min(count, this.count);
        current = ref Unsafe.Add(ref current, count);
        this.count -= count;
        return count;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly RefEnumerator<T> GetEnumerator() => new(ref current, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly void IDisposable.Dispose() { }
}

[SkipLocalsInit]
public unsafe struct PtrIter<T>: Iterator<T>
where T: unmanaged {
    T* current;
    nuint count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PtrIter(T* start, nuint length) {
        current = start;
        count = length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T> GetEnumerator() => new(current, count);

    public readonly nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out T item) {
        if (count != 0) {
            count--;
            item = *current++;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint AdvanceBy(nuint count) {
        count = Math.Min(count, this.count);
        this.count -= count;
        current += count;
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly void IDisposable.Dispose() { }
}

public ref struct IterEnumerator<T, U>(T iter): IEnumerator<U>
where T: Iterator<U>, allows ref struct
where U: allows ref struct {
    T iter = iter;
    U current = default!;

    public U Current {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => current;
        private set => current = value;
    }

    readonly object IEnumerator.Current => throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() => iter.Next(out current);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();

    readonly void IEnumerator.Reset() => throw new NotSupportedException();
}
