using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Allocators;

public static class Allocators {
    public static Managed GC => default;
    public static Pool Pool => default;
    public static Global Global => default;
    public static Jermalloc Jemalloc => default;
}

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

// TODO: AllocAligned
public unsafe interface NativeAllocator {
    static abstract void* Alloc(nuint size);
    static abstract void* Realloc(void* ptr, nuint newSize);
    static abstract void Free(void* ptr);
}

public struct Managed: ManagedAllocator, ScopedAllocator {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] AllocArray<T>(int length) => new T[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T[] ReallocArray<T>(T[]? array, int newLength) {
        var result = new T[newLength];

        // FIXME: Invalid code! Will write out of bounds!
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

public struct Pool: ManagedAllocator {
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

public unsafe readonly struct Global: NativeAllocator {
    [SupportedOSPlatform("windows")]
    static class Ucrt {
        const string Name = "ucrtbase";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* MallocGuard(nuint size) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "malloc")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void* Malloc(nuint size);

            return Malloc(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ReallocGuard(void* ptr, nuint newSize) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "realloc")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void* Realloc(void* ptr, nuint newSize);

            return Realloc(ptr, newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeGuard(void* ptr) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "free")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void Free(void* ptr);

            Free(ptr);
        }
    }

    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    [SupportedOSPlatform("freebsd")]
    static class Libc {
        const string Name = "libc";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* MallocGuard(nuint size) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "malloc")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void* Malloc(nuint size);

            return Malloc(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* ReallocGuard(void* ptr, nuint newSize) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "realloc")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void* Realloc(void* ptr, nuint newSize);

            return Realloc(ptr, newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeGuard(void* ptr) {
            [SuppressGCTransition]
            [DllImport(Name, EntryPoint = "free")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static extern void Free(void* ptr);

            Free(ptr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Alloc(nuint size) {
        if (OperatingSystem.IsWindows()) {
            return Ucrt.MallocGuard(size);
        } else if (
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            return Libc.MallocGuard(size);
        } else {
            return NativeMemory.Alloc(size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Realloc(void* ptr, nuint newSize) {
        if (OperatingSystem.IsWindows()) {
            return Ucrt.ReallocGuard(ptr, newSize);
        } else if (
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            return Libc.ReallocGuard(ptr, newSize);
        } else {
            return NativeMemory.Realloc(ptr, newSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(void* ptr) {
        if (OperatingSystem.IsWindows()) {
            Ucrt.FreeGuard(ptr);
        } else if (
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsFreeBSD()
        ) {
            Libc.FreeGuard(ptr);
        } else {
            NativeMemory.Free(ptr);
        }
    }
}

public unsafe struct Jermalloc { /*: NativeAllocator { /*
    ⠀⠀⠀⡯⡯⡾⠝⠘⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢊⠘⡮⣣⠪⠢⡑⡌
    ⠀⠀⠀⠟⠝⠈⠀⠀⠀⠡⠀⠠⢈⠠⢐⢠⢂⢔⣐⢄⡂⢔⠀⡁⢉⠸⢨⢑⠕⡌
    ⠀⠀⡀⠁⠀⠀⠀⡀⢂⠡⠈⡔⣕⢮⣳⢯⣿⣻⣟⣯⣯⢷⣫⣆⡂⠀⠀⢐⠑⡌
    ⢀⠠⠐⠈⠀⢀⢂⠢⡂⠕⡁⣝⢮⣳⢽⡽⣾⣻⣿⣯⡯⣟⣞⢾⢜⢆⠀⡀⠀⠪
    ⣬⠂⠀⠀⢀⢂⢪⠨⢂⠥⣺⡪⣗⢗⣽⢽⡯⣿⣽⣷⢿⡽⡾⡽⣝⢎⠀⠀⠀⢡
    ⣿⠀⠀⠀⢂⠢⢂⢥⢱⡹⣪⢞⡵⣻⡪⡯⡯⣟⡾⣿⣻⡽⣯⡻⣪⠧⠑⠀⠁⢐
    ⣿⠀⠀⠀⠢⢑⠠⠑⠕⡝⡎⡗⡝⡎⣞⢽⡹⣕⢯⢻⠹⡹⢚⠝⡷⡽⡨⠀⠀⢔
    ⣿⡯⠀⢈⠈⢄⠂⠂⠐⠀⠌⠠⢑⠱⡱⡱⡑⢔⠁⠀⡀⠐⠐⠐⡡⡹⣪⠀⠀⢘
    ⣿⣽⠀⡀⡊⠀⠐⠨⠈⡁⠂⢈⠠⡱⡽⣷⡑⠁⠠⠑⠀⢉⢇⣤⢘⣪⢽⠀⢌⢎
    ⣿⢾⠀⢌⠌⠀⡁⠢⠂⠐⡀⠀⢀⢳⢽⣽⡺⣨⢄⣑⢉⢃⢭⡲⣕⡭⣹⠠⢐⢗
    ⣿⡗⠀⠢⠡⡱⡸⣔⢵⢱⢸⠈⠀⡪⣳⣳⢹⢜⡵⣱⢱⡱⣳⡹⣵⣻⢔⢅⢬⡷
    ⣷⡇⡂⠡⡑⢕⢕⠕⡑⠡⢂⢊⢐⢕⡝⡮⡧⡳⣝⢴⡐⣁⠃⡫⡒⣕⢏⡮⣷⡟
    ⣷⣻⣅⠑⢌⠢⠁⢐⠠⠑⡐⠐⠌⡪⠮⡫⠪⡪⡪⣺⢸⠰⠡⠠⠐⢱⠨⡪⡪⡰
    ⣯⢷⣟⣇⡂⡂⡌⡀⠀⠁⡂⠅⠂⠀⡑⡄⢇⠇⢝⡨⡠⡁⢐⠠⢀⢪⡐⡜⡪⡊
    ⣿⢽⡾⢹⡄⠕⡅⢇⠂⠑⣴⡬⣬⣬⣆⢮⣦⣷⣵⣷⡗⢃⢮⠱⡸⢰⢱⢸⢨⢌
    ⣯⢯⣟⠸⣳⡅⠜⠔⡌⡐⠈⠻⠟⣿⢿⣿⣿⠿⡻⣃⠢⣱⡳⡱⡩⢢⠣⡃⠢⠁
    ⡯⣟⣞⡇⡿⣽⡪⡘⡰⠨⢐⢀⠢⢢⢄⢤⣰⠼⡾⢕⢕⡵⣝⠎⢌⢪⠪⡘⡌⠀
    ⡯⣳⠯⠚⢊⠡⡂⢂⠨⠊⠔⡑⠬⡸⣘⢬⢪⣪⡺⡼⣕⢯⢞⢕⢝⠎⢻⢼⣀⠀
    ⠁⡂⠔⡁⡢⠣⢀⠢⠀⠅⠱⡐⡱⡘⡔⡕⡕⣲⡹⣎⡮⡏⡑⢜⢼⡱⢩⣗⣯⣟
    ⢀⢂⢑⠀⡂⡃⠅⠊⢄⢑⠠⠑⢕⢕⢝⢮⢺⢕⢟⢮⢊⢢⢱⢄⠃⣇⣞⢞⣞⢾
    ⢀⠢⡑⡀⢂⢊⠠⠁⡂⡐⠀⠅⡈⠪⠪⠪⠣⠫⠑⡁⢔⠕⣜⣜⢦⡰⡎⡯⡾⡽ */
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
    public static unsafe T* AllocPtr<T>(nuint size) where T : unmanaged {
        return (T*)Malloc(size * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe T* ReallocPtr<T>(T* ptr, nuint newSize) where T : unmanaged {
        return (T*)Realloc(ptr, newSize * (nuint)sizeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void FreePtr<T>(T* ptr) where T : unmanaged {
        Free(ptr);
    }
}
