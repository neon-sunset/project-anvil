using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MimallocImpl = TerraFX.Interop.Mimalloc.Mimalloc;

namespace System.Allocators;

public static class Allocators {
    public static GC GC => default;
    public static Pool Pool => default;
    public static Mimalloc Mimalloc => default;
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
    static abstract T* AllocPtr<T>(nuint count) where T : unmanaged;
    static abstract T* ReallocPtr<T>(T* ptr, nuint newCount) where T : unmanaged;
    static abstract void FreePtr<T>(T* ptr) where T : unmanaged;
}

public struct GC: ManagedAllocator, ScopedAllocator {
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

    public static void FreeArray<T>(T[]? array) {
        if (array != null) {
            ArrayPool<T>.Shared.Return(array);
        }
    }
}

public unsafe struct Mimalloc: NativeAllocator, ScopedAllocator {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe T* AllocPtr<T>(nuint count) where T : unmanaged {
        return (T*)MimallocImpl.mi_malloc(count * (nuint)sizeof(T));
    }

    public static unsafe T* ReallocPtr<T>(T* ptr, nuint newSize) where T : unmanaged {
        return (T*)MimallocImpl.mi_realloc(ptr, newSize * (nuint)sizeof(T));
    }

    public static unsafe void FreePtr<T>(T* ptr) where T : unmanaged {
        MimallocImpl.mi_free(ptr);
    }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref T AllocRange<T>(nuint length) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        return ref Unsafe.AsRef<T>(MimallocImpl.mi_malloc(length * (nuint)sizeof(T)));
    }

    public static ref T ReallocRange<T>(ref T range, nuint oldLength, nuint newLength) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        return ref Unsafe.AsRef<T>(MimallocImpl.mi_realloc(Unsafe.AsPointer(ref range), newLength * (nuint)sizeof(T)));
    }

    public static void FreeRange<T>(ref T range) {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>(), false);

        MimallocImpl.mi_free(Unsafe.AsPointer(ref range));
    }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
}

public unsafe struct Jermalloc: NativeAllocator { /*
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
