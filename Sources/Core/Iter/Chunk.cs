using System.Allocators;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Iter;

// TODO: NSpan, NRange, NIndex
// public unsafe ref struct NChunkView<T, U, A>(T iter, nuint size): Iter<NSpan<U>>
// where T: Iter<U>, allows ref struct
// where U: unmanaged
// where A: NativeAllocator {
//     T iter = iter;
//     U* chunk = (U*)A.Alloc(size * (nuint)sizeof(U));
//     nuint index = 0;

//     public nuint? Count {
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         get {
//             if (iter.Count is nuint count) {
//                 var (cnt, rem) = Math.DivRem(count, size);
//                 return cnt + (rem > 0 ? (nuint)1 : 0);
//             }
//             return null;
//         }
//     }

//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public bool Next(out U item) {
//         while (index < size && iter.Next(out var value)) {
//             chunk[index++] = value;
//         }

//     }
// }

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Chunk<T> Chunk<T>(this Span<T> span, int chunkLength)
        => new(span, chunkLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Chunk<T> Chunk<T>(this ReadOnlySpan<T> span, int chunkLength)
        => new(span, chunkLength);
}

public ref struct Chunk<T>: Iter<ReadOnlySpan<T>> {
    ref T ptr;
    int length;
    int size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Chunk(ReadOnlySpan<T> span, int chunkLength) {
        ArgumentOutOfRangeException
            .ThrowIfNegativeOrZero(chunkLength);

        ptr = ref MemoryMarshal.GetReference(span);
        length = span.Length;
        size = chunkLength;
    }

    public readonly nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            var (cnt, rem) = Math.DivRem((uint)length, (uint)size);
            return cnt + (rem > 0 ? (nuint)1 : 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out ReadOnlySpan<T> item) {
        if (length > 0) {
            var len = Math.Min(length, size);
            item = MemoryMarshal.CreateReadOnlySpan(ref ptr, len);
            ptr = ref Unsafe.Add(ref ptr, (uint)len);
            length -= len;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IterEnumerator<Chunk<T>, ReadOnlySpan<T>> GetEnumerator() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Dispose() { }
}

public ref struct ChunkView<T, U, A>(T iter, int size): Iter<Span<U>>
where T: Iter<U>
where A: ManagedAllocator {
    T iter = iter;
    U[] chunk = A.AllocArray<U>(size);

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (iter.Count is nuint count) {
                var (cnt, rem) = Math.DivRem(count, (nuint)size);
                return cnt + (rem > 0 ? (nuint)1 : 0);
            }
            return null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out Span<U> item) {
        var cnt = 0;
        var buf = chunk;
        while (cnt < buf.Length &&
            iter.Next(out buf[cnt++])) ;
        item = buf.AsSpan(0, cnt);
        return cnt > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        A.FreeArray(chunk);
        iter.Dispose();
    }
}
