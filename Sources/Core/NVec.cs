using System.Allocators;
using System.Collections;
using System.Iter;
using System.Runtime.CompilerServices;
using Anvil.Core;

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
public unsafe struct NVec<T, A>:
    As<PtrIter<T>>,
    As<RefIter<T>>,
    IList<T>,
    IDisposable
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
        items = (T*)A.Alloc(capacity * (nuint)sizeof(T));
        count = 0;
        this.capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVec(ReadOnlySpan<T> source) {
        var ptr = (T*)A.Alloc((uint)source.Length * (nuint)sizeof(T));
        source.CopyTo(new(ptr, source.Length));

        items = ptr;
        count = (uint)source.Length;
        capacity = (uint)source.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NVec(T value, nuint count) {
        var ptr = (T*)A.Alloc(count * (nuint)sizeof(T));
        Memory.Fill(ref Unsafe.AsRef<T>(ptr), value, count);

        items = ptr;
        this.count = count;
        capacity = count;
    }

    public readonly ref T this[nuint index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.AsRef<T>(
            index < count ? items + index : Throw.IndexOutOfRange());
    }

    public readonly nuint Count => count;

    readonly T IList<T>.this[int index] {
        get => this[(uint)index];
        set => this[(uint)index] = value;
    }

    readonly int ICollection<T>.Count => checked((int)count);
    readonly bool ICollection<T>.IsReadOnly => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => new(items, checked((int)count));

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NVec<T, A> Collect<U>(U iter)
    where U: Iterator<T>, allows ref struct {

        return iter.Count switch {
            null => Uncounted(iter),
            nuint n when n > 0 => Counted(iter),
            _ => default
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static NVec<T, A> Counted(U iter) {
            var cap = iter.Count!.Value;
            var ptr = (T*)A.Alloc(cap * (nuint)sizeof(T));
            var cnt = (nuint)0;

            while (iter.Next(out var item)) {
                ptr[cnt++] = item;
            }

            return new() {
                items = ptr,
                count = cnt,
                capacity = cap
            };
        }

        static NVec<T, A> Uncounted(U iter) {
            var vec = new NVec<T, A>();
            while (iter.Next(out var item)) {
                vec.Add(item);
            }
            return vec;
        }
    }


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
    public readonly PtrIter<T> Iter() => new(items, count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly PtrEnumerator<T> GetEnumerator() => new(items, count);

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
    public static implicit operator Slice<T>(NVec<T, A> source) => new(source.items, source.count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MutSlice<T>(NVec<T, A> source) => new(source.items, source.count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(NVec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(NVec<T, A> source) => source.AsSpan();

    readonly PtrIter<T> As<PtrIter<T>>.As() => new(items, count);
    readonly RefIter<T> As<RefIter<T>>.As() => new(ref Unsafe.AsRef<T>(items), count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        var ptr = items;
        if (ptr != null) {
            items = null;
            count = 0;
            capacity = 0;
            A.Free(ptr);
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
        ptr = (T*)A.Realloc(ptr, cap * (nuint)sizeof(T));
        ptr[cnt++] = item;

        items = ptr;
        count = cnt;
        capacity = cap;
    }
}
