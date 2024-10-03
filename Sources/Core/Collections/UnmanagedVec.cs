using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Anvil.Core.Collections;

public static class UnmanagedVec {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UnmanagedVec<T, A> Create<T, A>(ReadOnlySpan<T> items)
    where T: unmanaged
    where A: UnmanagedAllocator => new(items);
}

[SkipLocalsInit]
[CollectionBuilder(typeof(UnmanagedVec), nameof(UnmanagedVec.Create))]
public unsafe struct UnmanagedVec<T, A>: IList<T>, IDisposable
where T: unmanaged
where A: UnmanagedAllocator {
    static nuint MinSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 128 / (nuint)sizeof(T);
    }

    internal T* items;
    internal nuint count;
    internal nuint capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnmanagedVec() {
        items = null;
        count = 0;
        capacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnmanagedVec(nuint capacity) {
        items = A.Alloc<T>(capacity);
        count = 0;
        this.capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnmanagedVec(ReadOnlySpan<T> source) {
        var ptr = A.Alloc<T>((uint)source.Length);
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

    public void Add(T item) {
        var cnt = count;
        if (cnt >= capacity)
            goto Grow;
        Add:
        items[cnt++] = item;
        count = cnt;
        return;
    Grow:
        Grow();
        goto Add;
    }

    public void Clear() => count = 0;

    public readonly bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public readonly void CopyTo(T[] array, int arrayIndex) {
        AsSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly UnmanagedEnumerator<T> GetEnumerator() => new(items, count);

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
    public static implicit operator Span<T>(UnmanagedVec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(UnmanagedVec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        if (items != null) {
            A.Free(items);
            items = null;
            count = 0;
            capacity = 0;
        }
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [MemberNotNull(nameof(items))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Grow() {
        if (items != null) {
            items = A.Realloc(items, capacity * 2);
        }
        else {
            items = A.Alloc<T>(MinSize);
        }
    }
}
