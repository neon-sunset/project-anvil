using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Anvil.Core;

public interface ManagedAllocator {
    static abstract T[] AllocArray<T>(int length);
    static abstract T[] ReallocArray<T>(T[] array, int newLength);
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

public unsafe interface NativeAllocator {
    static abstract T* Alloc<T>(nuint size) where T : unmanaged;
    static abstract T* Realloc<T>(T* ptr, nuint newSize) where T : unmanaged;
    static abstract void Free<T>(T* ptr) where T : unmanaged;
}

public readonly struct GC: ManagedAllocator, ScopedAllocator {
    public static T[] AllocArray<T>(int length) => new T[length];

    public static T[] ReallocArray<T>(T[] array, int newLength) {
        var result = new T[newLength];
        array.AsSpan().CopyTo(result);
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

    public static T[] ReallocArray<T>(T[] array, int newLength) {
        var result = ArrayPool<T>.Shared.Rent(newLength);
        array.AsSpan().CopyTo(result);
        ArrayPool<T>.Shared.Return(array);
        return result;
    }

    public static void FreeArray<T>(T[]? array) {
        if (array != null) {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

public readonly unsafe struct Global: NativeAllocator, ScopedAllocator {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T* Alloc<T>(nuint size) where T : unmanaged {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            return (T*)Posix.Malloc(size * (nuint)sizeof(T));
        }

        return (T*)NativeMemory.Alloc(size * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T* Realloc<T>(T* ptr, nuint newSize) where T : unmanaged {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            return (T*)Posix.Realloc(ptr, newSize * (nuint)sizeof(T));
        }

        return (T*)NativeMemory.Realloc(ptr, newSize * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Free<T>(T* ptr) where T : unmanaged {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            Posix.Free(ptr);
            return;
        }

        NativeMemory.Free(ptr);
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T AllocRange<T>(nuint length) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            return ref Unsafe.AsRef<T>(Posix.Malloc(length * (nuint)sizeof(T)));
        }

        return ref Unsafe.AsRef<T>(NativeMemory.Alloc(length * (nuint)sizeof(T)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        var ptr = Unsafe.AsPointer(ref range);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            return ref Unsafe.AsRef<T>(Posix.Realloc(ptr, newLength * (nuint)sizeof(T)));
        }

        return ref Unsafe.AsRef<T>(
            NativeMemory.Realloc(ptr, newLength * (nuint)sizeof(T)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeRange<T>(ref T range) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()) {
            Posix.Free(Unsafe.AsPointer(ref range));
            return;
        }

        NativeMemory.Free(Unsafe.AsPointer(ref range));
    }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}

[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
static unsafe partial class Posix {
    [SuppressGCTransition]
    [LibraryImport("libc", EntryPoint = "malloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial void* Malloc(nuint size);

    [SuppressGCTransition]
    [LibraryImport("libc", EntryPoint = "realloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial void* Realloc(void* ptr, nuint newSize);

    [SuppressGCTransition]
    [LibraryImport("libc", EntryPoint = "free")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static partial void Free(void* ptr);
}
