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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] AllocArray<T>(int length) => new T[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T[] ReallocArray<T>(T[]? array, int newLength) {
        var result = new T[newLength];

        var src = array.AsSpan();
        var dst = MemoryMarshal.CreateSpan(
            ref MemoryMarshal.GetArrayDataReference(result), src.Length);
        src.CopyTo(dst);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeArray<T>(T[]? array) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AllocRange<T>(nuint length) =>
        ref MemoryMarshal.GetArrayDataReference(new T[length]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        var result = new T[newLength];
        MemoryMarshal
            .CreateSpan(ref range, (int)(uint)Math.Min(oldLength, newLength))
            .CopyTo(result);
        return ref MemoryMarshal.GetArrayDataReference(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeRange<T>(ref T range) { }
}

public readonly struct Pool: ManagedAllocator {
    public static T[] AllocArray<T>(int length) => ArrayPool<T>.Shared.Rent(length);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T[] ReallocArray<T>(T[] array, int newLength) {
        var result = ArrayPool<T>.Shared.Rent(newLength);

        var src = array.AsSpan();
        var dst = MemoryMarshal.CreateSpan(
            ref MemoryMarshal.GetArrayDataReference(result), src.Length);
        src.CopyTo(dst);

        ArrayPool<T>.Shared.Return(array);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FreeArray<T>(T[]? array) {
        if (array != null) {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

public readonly unsafe partial struct Global: NativeAllocator, ScopedAllocator {
    [SupportedOSPlatform("windows")]
    static partial class Ucrt {
        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("ucrtbase", EntryPoint = "malloc")]
        public static partial void* Malloc(nuint size);

        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("ucrtbase", EntryPoint = "realloc")]
        public static partial void* Realloc(void* ptr, nuint newSize);

        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("ucrtbase", EntryPoint = "free")]
        public static partial void Free(void* ptr);
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("windows")]
    static partial class Libc {
        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("libc", EntryPoint = "malloc")]
        public static partial void* Malloc(nuint size);

        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("libc", EntryPoint = "realloc")]
        public static partial void* Realloc(void* ptr, nuint newSize);

        [SuppressGCTransition]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [LibraryImport("libc", EntryPoint = "free")]
        public static partial void Free(void* ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Alloc<T>(nuint size) where T : unmanaged {
        if (OperatingSystem.IsWindows()) {
            return (T*)Ucrt.Malloc(size * (nuint)sizeof(T));
        }

        // if (OperatingSystem.IsLinux() ||
        //     OperatingSystem.IsMacOS() ||
        //     OperatingSystem.IsFreeBSD()
        // ) {
        //     return (T*)Libc.Malloc(size * (nuint)sizeof(T));
        // }

        return (T*)NativeMemory.Alloc(size * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Realloc<T>(T* ptr, nuint newSize) where T : unmanaged {
        if (OperatingSystem.IsWindows()) {
            return (T*)Ucrt.Realloc(ptr, newSize * (nuint)sizeof(T));
        }

        // if (OperatingSystem.IsLinux() ||
        //     OperatingSystem.IsMacOS() ||
        //     OperatingSystem.IsFreeBSD()
        // ) {
        //     return (T*)Libc.Realloc(ptr, newSize * (nuint)sizeof(T));
        // }

        return (T*)NativeMemory.Realloc(ptr, newSize * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free<T>(T* ptr) where T : unmanaged {
        if (OperatingSystem.IsWindows()) {
            Ucrt.Free(ptr);
            return;
        }

        // if (OperatingSystem.IsLinux() ||
        //     OperatingSystem.IsMacOS() ||
        //     OperatingSystem.IsFreeBSD()
        // ) {
        //     Libc.Free(ptr);
        //     return;
        // }

        NativeMemory.Free(ptr);
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AllocRange<T>(nuint length) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        if (OperatingSystem.IsWindows()) {
            return ref Unsafe.AsRef<T>(Ucrt.Malloc(length * (nuint)sizeof(T)));
        }

        if (OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            return ref Unsafe.AsRef<T>(Libc.Malloc(length * (nuint)sizeof(T)));
        }

        return ref Unsafe.AsRef<T>(NativeMemory.Alloc(length * (nuint)sizeof(T)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        if (OperatingSystem.IsWindows()) {
            return ref Unsafe.AsRef<T>(Ucrt.Realloc(Unsafe.AsPointer(ref range), newLength * (nuint)sizeof(T)));
        }

        if (OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            return ref Unsafe.AsRef<T>(Libc.Realloc(Unsafe.AsPointer(ref range), newLength * (nuint)sizeof(T)));
        }

        return ref Unsafe.AsRef<T>(NativeMemory.Realloc(Unsafe.AsPointer(ref range), newLength * (nuint)sizeof(T)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeRange<T>(ref T range) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        if (OperatingSystem.IsWindows()) {
            Ucrt.Free(Unsafe.AsPointer(ref range));
            return;
        }

        if (OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            Libc.Free(Unsafe.AsPointer(ref range));
            return;
        }

        NativeMemory.Free(Unsafe.AsPointer(ref range));
    }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}

public readonly unsafe partial struct Jemalloc: NativeAllocator {
    const string Name = "jemalloc";

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "malloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void* Malloc(nuint size);

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "realloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void* Realloc(void* ptr, nuint newSize);

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "free")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void Free(void* ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Alloc<T>(nuint size) where T : unmanaged {
        return (T*)Malloc(size * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Realloc<T>(T* ptr, nuint newSize) where T : unmanaged {
        return (T*)Realloc((void*)ptr, newSize * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free<T>(T* ptr) where T : unmanaged {
        Free((void*)ptr);
    }
}

public readonly unsafe partial struct Mimalloc: NativeAllocator {
    const string Name = "mimalloc";

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "malloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void* Malloc(nuint size);

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "realloc")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void* Realloc(void* ptr, nuint newSize);

    [SuppressGCTransition]
    [DllImport(Name, EntryPoint = "free")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static extern void Free(void* ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Alloc<T>(nuint size) where T : unmanaged {
        return (T*)Malloc(size * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* Realloc<T>(T* ptr, nuint newSize) where T : unmanaged {
        return (T*)Realloc((void*)ptr, newSize * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free<T>(T* ptr) where T : unmanaged {
        Free((void*)ptr);
    }
}
