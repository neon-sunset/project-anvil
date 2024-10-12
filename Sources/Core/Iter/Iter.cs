using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Iter;

public static unsafe partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIter<T> Iter<T>(
        [UnscopedRef] params Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIter<T> Iter<T>(
        [UnscopedRef] params ReadOnlySpan<T> span) => new(span);
}

public static partial class OpsExt {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIter<T> Iter<T>(this Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanIter<T> Iter<T>(this ReadOnlySpan<T> span) => new(span);
}

public interface Iter<T>: IDisposable
where T: allows ref struct {
    nuint? Count { get; }
    bool Next(out T item);
}

public interface ConvIter<U> {
    static abstract U From<T, V>(T iter)
    where T: Iter<V>, allows ref struct
    where V: allows ref struct;
}

[SkipLocalsInit]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public ref struct SpanIter<T>(ReadOnlySpan<T> values):
    Iter<T>,
    As<ReadOnlySpan<T>>
{
    readonly ref T ptr = ref MemoryMarshal.GetReference(values);
    int index = 0;
    readonly int length = values.Length;

    public readonly nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out T item) {
        var i = index;
        if (i < length) {
            item = Unsafe.Add(ref ptr, (uint)index);
            index = i + 1;
            return true;
        }
        item = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> As() {
        return MemoryMarshal.CreateReadOnlySpan(ref ptr, length);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<SpanIter<T>, T> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SpanIter<T>(Span<T> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SpanIter<T>(ReadOnlySpan<T> span) => new(span);

    readonly void IDisposable.Dispose() { }
}

[SkipLocalsInit]
public ref struct RefIter<T>: Iter<T> {
    ref T current;
    readonly ref T end;

    public readonly nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nuint)Unsafe.ByteOffset(ref current, ref end) / (nuint)Unsafe.SizeOf<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefIter(ReadOnlySpan<T> span) {
        current = ref MemoryMarshal.GetReference(span);
        end = ref Unsafe.Add(ref current, (uint)span.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefIter(ref T start, nuint length) {
        current = ref start;
        end = ref Unsafe.Add(ref start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out T item) {
        if (Unsafe.IsAddressLessThan(ref current, ref end)) {
            item = current;
            current = ref Unsafe.Add(ref current, 1);
            return true;
        }
        item = default!;
        return false;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<RefIter<T>, T> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    readonly void IDisposable.Dispose() { }
}

public ref struct IterEnumerator<T, U>(T iter): IEnumerator<U>
where T: Iter<U>, allows ref struct
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
