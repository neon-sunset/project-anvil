using System.Allocators;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public static class Vec {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, A> New<T, A>(ReadOnlySpan<T> source)
    where A: ManagedAllocator => new(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, A> New<T, A>(ReadOnlySpan<T> source, A _)
    where A: ManagedAllocator => new(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, Allocators.GC> Wrap<T>(T[] items) {
        return new() {
            items = items,
            count = items.Length
        };
    }

    public struct Enumerator<T>: IEnumerator<T> {
        readonly T[] items;
        readonly uint end;
        uint index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(T[]? array, int count) {
            items = array!;
            end = (uint)count;
        }

        public T Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get;
            private set;
        } = default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() {
            var offset = index;
            if (offset < end) {
                Current = Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(items), offset);
                index = offset + 1;
                return true;
            }

            return false;
        }

        public void Reset() {
            index = 0;
            Current = default!;
        }

        readonly void IDisposable.Dispose() { }
        readonly object? IEnumerator.Current => Current;
    }
}

[SkipLocalsInit]
[CollectionBuilder(typeof(Vec), nameof(Vec.New))]
public struct Vec<T, A>: IList<T>, IDisposable
where A: ManagedAllocator {
    static int MinSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (typeof(A) == typeof(Pool) ? 128 : 32) / Unsafe.SizeOf<T>();
    }

    internal T[] items;
    internal int count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec() {
        items = typeof(A) == typeof(GC) ? [] : A.AllocArray<T>(MinSize);
        count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec(int capacity) {
        items = A.AllocArray<T>(capacity);
        count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec(ReadOnlySpan<T> source) {
        var array = A.AllocArray<T>(source.Length);
        source.CopyTo(MemoryMarshal.CreateSpan(
            ref MemoryMarshal.GetArrayDataReference(array),
            source.Length));

        items = array;
        count = source.Length;
    }

    public readonly ref T this[int index] {
        get {
            var offset = (uint)index;
            ArgumentOutOfRangeException
                .ThrowIfGreaterThanOrEqual(offset, (uint)count);
            return ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(items!), offset);
        }
    }

    readonly T IList<T>.this[int index] {
        get => this[index];
        set => this[index] = value;
    }

    public readonly int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => count;
    }

    public int Capacity {
        readonly get => items?.Length ?? 0;
        set {
            var (arr, cnt) = (items, count);
            ArgumentOutOfRangeException
                .ThrowIfGreaterThan((uint)value, (uint)cnt);
            if (value != arr.Length) {
                items = value is 0
                    ? A.ReallocArray(arr, value)
                    : typeof(A) == typeof(GC) ? [] : A.AllocArray<T>(0);
            }
        }
    }

    readonly bool ICollection<T>.IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() {
        ref var ptr = ref Unsafe.NullRef<T>();
        var arr = items;
        if (arr != null) {
            ptr = ref MemoryMarshal.GetArrayDataReference(arr)!;
        }
        return MemoryMarshal.CreateSpan(ref ptr, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        var arr = items;
        var cnt = count;
        if ((uint)cnt < (uint)arr.Length) {
            arr[cnt] = item;
            count = cnt + 1;
        } else {
            AddGrow(item);
        }
    }

    public void Clear() {
        count = 0;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            items.AsSpan().Clear();
        }
    }

    public readonly bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public readonly void CopyTo(T[] array, int arrayIndex) {
        AsSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vec.Enumerator<T> GetEnumerator() => new(items, Count);

    public readonly int IndexOf(T item) =>
        items != null ? Array.IndexOf(items, item, 0, count) : -1;

    public void Insert(int index, T item) {
        throw new NotImplementedException();
    }

    public T Pop() {
        if (count < 1) {
            throw new InvalidOperationException("Empty Vec");
        }
        return items![--count];
    }

    public bool Remove(T item) {
        var index = IndexOf(item);
        if (index >= 0) {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) {
        ArgumentOutOfRangeException
            .ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);

        count--;
        if (index < count) {
            AsSpan()[(index + 1)..].CopyTo(AsSpan()[index..]);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            items![count] = default!;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> Slice(int start) => AsSpan().Slice(start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> Slice(int start, int length) => AsSpan().Slice(start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddGrow(T item) {
        var arr = items;
        var cnt = count;
        var cap = cnt != 0 ? cnt * 2 : MinSize;

        arr = A.ReallocArray(arr, cap);

        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(arr), (uint)cnt++) = item;
        items = arr;
        count = cnt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(Vec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(Vec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        if (typeof(A) == typeof(GC)) return;
        var array = items;
        if (array != null) {
            items = null!;
            count = 0;
            A.FreeArray(array);
        }
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
