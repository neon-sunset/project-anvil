using System.Allocators;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System;

public static class NVec {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, A> New<T, A>(ReadOnlySpan<T> items)
    where T: unmanaged
    where A: NativeAllocator => new(items);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, A> New<T, A>(ReadOnlySpan<T> items, A _)
    where T: unmanaged
    where A: NativeAllocator => new(items);
}

[SkipLocalsInit]
[CollectionBuilder(typeof(NVec), nameof(NVec.New))]
public unsafe struct NVec<T, A>: IList<T>, IDisposable
where T: unmanaged
where A: NativeAllocator {
    static nuint MinSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 128 / (nuint)sizeof(T);
    }

    internal T* items;
    internal nuint count;
    internal nuint capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVec() {
        items = null;
        count = 0;
        capacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVec(nuint capacity) {
        items = A.AllocPtr<T>(capacity);
        count = 0;
        this.capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVec(ReadOnlySpan<T> source) {
        var ptr = A.AllocPtr<T>((uint)source.Length);
        source.CopyTo(new(ptr, source.Length));

        items = ptr;
        count = (uint)source.Length;
        capacity = (uint)source.Length;
    }

    public readonly ref T this[nuint index] {
        get {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
            return ref Unsafe.AsRef<T>(items + index);
        }
    }

    public readonly nuint Count => count;

    readonly T IList<T>.this[int index] {
        get => this[(uint)index];
        set => this[(uint)index] = value;
    }

    readonly int ICollection<T>.Count => int.CreateChecked(count);
    readonly bool ICollection<T>.IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => new(items, int.CreateChecked(count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        var cnt = count;
        if (cnt < capacity) {
            items[cnt++] = item;
            count = cnt;
        } else {
            AddGrow(item);
        }
    }

    public void Clear() => count = 0;

    public readonly bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public readonly void CopyTo(T[] array, int arrayIndex) {
        AsSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly NEnumerator<T> GetEnumerator() => new(items, count);

    public readonly int IndexOf(T item) {
        // TODO: Write optimal dispatch here
        throw new NotImplementedException();
    }

    public void Insert(int index, T item) {
        throw new NotImplementedException();
    }

    public T Pop() {
        var cnt = count;
        if (cnt is 0) {
            throw new InvalidOperationException("Empty Vec");
        }
        var item = items[--cnt];
        count = cnt;
        return item;
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
            .ThrowIfGreaterThanOrEqual((uint)index, count);

        count--;
        if ((uint)index < count) {
            Buffer.MemoryCopy(
                items + index + 1,
                items + index,
                capacity * (nuint)sizeof(T),
                (count - (uint)index) * (nuint)sizeof(T)
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(NVec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(NVec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        var ptr = items;
        if (ptr != null) {
            items = null;
            count = 0;
            capacity = 0;
            A.FreePtr(ptr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void Deconstruct(
        out T* items,
        out nuint count,
        out nuint capacity
    ) {
        items = this.items;
        count = this.count;
        capacity = this.capacity;
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void AddGrow(T item) {
        var (ptr, cnt, cap) = this;

        cap = cap != 0 ? cap * 2 : MinSize;
        ptr = A.ReallocPtr(ptr, cap);
        ptr[cnt++] = item;

        items = ptr;
        count = cnt;
        capacity = cap;
    }
}
