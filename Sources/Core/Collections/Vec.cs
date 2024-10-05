using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Anvil.Core.Collections;

public static class Vec {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, A> Create<T, A>(ReadOnlySpan<T> source)
    where A: ManagedAllocator => new(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, GC> ToVec<T>(this IEnumerable<T> source) {
        var items = source.ToArray();
        return new() {
            items = items,
            Count = items.Length
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec<T, GC> Wrap<T>(T[] items) {
        return new() {
            items = items,
            Count = items.Length
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
[CollectionBuilder(typeof(Vec), nameof(Vec.Create))]
public struct Vec<T, A>: IList<T>, IDisposable
where A: ManagedAllocator {
    static int MinSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (typeof(A) == typeof(Pool) ? 128 : 32) / Unsafe.SizeOf<T>();
    }

    internal T[]? items;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec() {
        items = null;
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec(int capacity) {
        items = A.AllocArray<T>(capacity);
        Count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec(ReadOnlySpan<T> source) {
        var array = A.AllocArray<T>(source.Length);
        source.CopyTo(MemoryMarshal.CreateSpan(
            ref MemoryMarshal.GetArrayDataReference(array),
            source.Length));

        items = array;
        Count = source.Length;
    }

    public readonly ref T this[int index] {
        get {
            ArgumentOutOfRangeException
                .ThrowIfGreaterThanOrEqual((uint)index, (uint)Count);
            return ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(items!), (nint)(uint)index);
        }
    }

    readonly T IList<T>.this[int index] {
        get => this[index];
        set => throw new NotImplementedException();
    }

    public int Count { readonly get; internal set; }

    public readonly bool IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() {
        ref var ptr = ref Unsafe.NullRef<T>();
        var arr = items;
        if (arr != null) {
            ptr = ref MemoryMarshal.GetArrayDataReference(arr)!;
        }
        return MemoryMarshal.CreateSpan(ref ptr, Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        var array = items;
        var count = Count;
        if (array != null && (uint)count < (uint)array.Length) {
            Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(array),
                (nint)(uint)count++) = item;
            Count = count;
            return;
        }

        AddGrow(item);
    }

    public void Clear() {
        Count = 0;
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
        items != null ? Array.IndexOf(items, item, 0, Count) : -1;

    public void Insert(int index, T item) {
        throw new NotImplementedException();
    }

    public T Pop() {
        if (Count == 0) {
            throw new InvalidOperationException("Empty Vec");
        }
        return items![--Count];
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

        Count--;
        if (index < Count) {
            AsSpan()[(index + 1)..].CopyTo(AsSpan()[index..]);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            items![Count] = default!;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void AddGrow(T item) {
        var array = items;
        var count = Count;

        if (array != null) {
            array = A.ReallocArray(array, count * 2);
        }
        else {
            array = A.AllocArray<T>(MinSize);
        }

        array[count++] = item;
        items = array;
        Count = count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(Vec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(Vec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        var array = Interlocked.Exchange(ref items, null);
        if (array != null) {
            items = null;
            Count = 0;
            A.FreeArray(array);
        }
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
