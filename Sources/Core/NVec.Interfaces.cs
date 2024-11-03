using System.Collections;
using System.Iter;
using System.Runtime.CompilerServices;

namespace System;

public unsafe partial struct NVec<T, A> {
    readonly PtrIter<T> As<PtrIter<T>>.As() => new(items, count);
    readonly RefIter<T> As<RefIter<T>>.As() => new(ref Unsafe.AsRef<T>(items), count);


    readonly T IList<T>.this[int index] {
        get => this[(uint)index];
        set => this[(uint)index] = value;
    }

    readonly int IList<T>.IndexOf(T item) {
        var index = Memory.IndexOfUnconstrained(ref *items, count, item);
        return index.HasValue ? checked((int)index.Value) : -1;
    }

    void IList<T>.Insert(int index, T item) {
        throw new NotImplementedException();
    }

    void IList<T>.RemoveAt(int index) {
        ArgumentOutOfRangeException
            .ThrowIfGreaterThanOrEqual((uint)index, count);

        count--;
        if ((uint)index < count) {
            Memory.Copy(
                items + index + 1,
                items + index,
                (count - (uint)index) * (nuint)sizeof(T)
            );
        }
    }

    readonly int ICollection<T>.Count => checked((int)count);
    readonly bool ICollection<T>.IsReadOnly => false;


    readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        => AsSpan().CopyTo(array.AsSpan(arrayIndex));

    bool ICollection<T>.Remove(T item) {
        var index = IndexOf(item);
        if (index >= 0) {
            Remove((uint)index).DisposeIfDisposable();
            return true;
        }
        return false;
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
