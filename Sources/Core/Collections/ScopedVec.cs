using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Anvil.Core.Collections;

public static class ScopedVec {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ScopedVec<T, A> Create<T, A>(ReadOnlySpan<T> items)
    where A: ScopedAllocator => new(items);
}

[SkipLocalsInit]
[CollectionBuilder(typeof(ScopedVec), nameof(ScopedVec.Create))]
public ref struct ScopedVec<T, A>: IList<T>, IDisposable
where A: ScopedAllocator {
    static nuint MinSize {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 128 / (nuint)Unsafe.SizeOf<T>();
    }

    internal ref T items;
    internal nuint count;
    internal nuint capacity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScopedVec() {
        items = ref Unsafe.NullRef<T>();
        count = 0;
        capacity = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScopedVec(nuint capacity) {
        items = ref A.AllocRange<T>(capacity);
        count = 0;
        this.capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ScopedVec(ReadOnlySpan<T> source) {
        ref var ptr = ref A.AllocRange<T>((uint)source.Length);
        source.CopyTo(MemoryMarshal.CreateSpan(ref ptr, source.Length));

        items = ref ptr!;
        count = (uint)source.Length;
        capacity = (uint)source.Length;
    }

    public readonly ref T this[nuint index] {
        get {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
            return ref Unsafe.Add(ref items, index);
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
    public readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref items, int.CreateChecked(count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item) {
        var cnt = count;
        if (cnt < capacity) {
            Unsafe.Add(ref items, cnt++) = item;
            count = cnt;
            return;
        }

        AddGrow(item);
    }

    public void Clear() {
        ref var ptr = ref items;
        var cnt = count;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            Memory.Clear(ref items, cnt);
        }
        count = 0;
    }

    public readonly bool Contains(T item) {
        return IndexOf(item) >= 0;
    }

    public readonly void CopyTo(T[] array, int arrayIndex) {
        AsSpan().CopyTo(array.AsSpan(arrayIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ScopedEnumerator<T> GetEnumerator() => new(ref items, count);

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
        var item = Unsafe.Add(ref items, --cnt);
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
            Memory.Copy(
                ref Unsafe.Add(ref items, (uint)index + 1),
                ref Unsafe.Add(ref items, (uint)index),
                count - (uint)index
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(ScopedVec<T, A> source) => source.AsSpan();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(ScopedVec<T, A> source) => source.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        ref var ptr = ref items;
        if (!Unsafe.IsNullRef(ref ptr)) {
            items = ref Unsafe.NullRef<T>();
            count = 0;
            capacity = 0;
            A.FreeRange(ref ptr);
        }
    }

    readonly IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotSupportedException();
    readonly IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    void AddGrow(T item) {
        ref var ptr = ref items;
        var cnt = count;
        var cap = capacity;

        if (cap != 0) {
            cap *= 2;
            ptr = ref A.ReallocRange(ref ptr, cnt, cap)!;
        }
        else {
            cap = MinSize;
            ptr = ref A.AllocRange<T>(cap)!;
        }

        Unsafe.Add(ref ptr, cnt++) = item;
        items = ref ptr!;
        count = cnt;
        capacity = cap;
    }
}
