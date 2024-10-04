using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Anvil.Core;

public interface ManagedAllocator {
    static abstract T[] AllocArray<T>(int length);
    static abstract T[] ReallocArray<T>(T[] array, int oldLength, int newLength);
    static abstract void FreeArray<T>(T[]? array);
}

public interface ScopedAllocator {
    static abstract ref T AllocRange<T>(nuint length);
    static abstract ref T ReallocRange<T>(
        ref T range,
        nuint oldLength,
        nuint newLength);
    static abstract void FreeRange<T>(ref T range);
}

public unsafe interface UnmanagedAllocator {
    static abstract T* Alloc<T>(nuint size) where T: unmanaged;
    static abstract T* Realloc<T>(T* ptr, nuint newSize) where T: unmanaged;
    static abstract void Free<T>(T* ptr) where T: unmanaged;
}

public readonly struct GC: ManagedAllocator, ScopedAllocator {
    public static T[] AllocArray<T>(int length) => new T[length];

    public static T[] ReallocArray<T>(T[] array, int oldLength, int newLength) {
        var result = new T[newLength];
        array
            .AsSpan(0, Math.Min(oldLength, newLength))
            .CopyTo(result);
        return result;
    }

    public static void FreeArray<T>(T[]? array) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AllocRange<T>(nuint length) =>
        ref MemoryMarshal.GetArrayDataReference(new T[length]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        var result = new T[newLength];
        MemoryMarshal
            .CreateSpan(ref range, (int)(uint)Math.Min(oldLength, newLength))
            .CopyTo(result);
        return ref MemoryMarshal.GetArrayDataReference(result);
    }

    public static void FreeRange<T>(ref T range) { }
}

public readonly struct Pool: ManagedAllocator {
    public static T[] AllocArray<T>(int length) => ArrayPool<T>.Shared.Rent(length);

    public static T[] ReallocArray<T>(T[] array, int oldLength, int newLength) {
        var result = ArrayPool<T>.Shared.Rent(newLength);
        array
            .AsSpan(0, Math.Min(oldLength, newLength))
            .CopyTo(result);
        ArrayPool<T>.Shared.Return(array);
        return result;
    }

    public static void FreeArray<T>(T[]? array) {
        if (array != null) {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

public readonly unsafe struct Native: UnmanagedAllocator, ScopedAllocator {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T* Alloc<T>(nuint size) where T: unmanaged =>
        (T*)NativeMemory.Alloc(size * (nuint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T* Realloc<T>(T* ptr, nuint newSize) where T: unmanaged =>
        (T*)NativeMemory.Realloc(ptr, newSize * (nuint)sizeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Free<T>(T* ptr) where T: unmanaged =>
        NativeMemory.Free(ptr);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T AllocRange<T>(nuint length) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        return ref Unsafe.AsRef<T>(NativeMemory.Alloc(length * (nuint)Unsafe.SizeOf<T>()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        var s = MemoryMarshal.CreateSpan(ref range, 1);
        fixed (T* ptr = s) {
            return ref Unsafe.AsRef<T>(
                NativeMemory.Realloc(ptr, newLength * (nuint)Unsafe.SizeOf<T>()));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeRange<T>(ref T range) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        NativeMemory.Free(Unsafe.AsPointer(ref range));
    }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}
