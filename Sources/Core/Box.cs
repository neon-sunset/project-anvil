// TODO: Add MIT license attribution to CommunityToolkit.HighPerformance

using System.Allocators;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

public static class Box {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GC<T> GC<T>(T value)
    where T: struct => Unsafe.As<GC<T>>(value);

    [return: NotNullIfNotNull(nameof(nullable))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GC<T>? GC<T>(T? nullable)
    where T: struct => nullable.HasValue ? Unsafe.As<GC<T>>(nullable.Value) : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T, A> New<T, A>(T value)
    where T: unmanaged
    where A: NativeAllocator => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T, A> New<T, A>(T value, A _)
    where T: unmanaged
    where A: NativeAllocator => new(value);
}

[DebuggerDisplay("{ToString(),raw}")]
public readonly unsafe struct Box<T, A>:
    AsUnscopedRef<T>,
    AsUnscopedMut<T>,
    IntoUnscoped<T>,
    Into<Span<T>>,
    Ctor<T, Box<T, A>>,
    IDisposable
where T: unmanaged
where A: NativeAllocator {
    readonly T* box;

    public readonly ref T Ref {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.AsRef<T>(box);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Box(T value) {
        var ptr = (T*)A.Alloc((nuint)sizeof(T));
        *ptr = value;
        box = ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T, A> New(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Box<T, A> value) => value.box;

    ref readonly T AsRef<T>.Ref => ref Ref;
    T Into<T>.Into() => *box;
    Span<T> Into<Span<T>>.Into() => new(ref Ref);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        var ptr = box;
        if (default(T) is IDisposable) {
            ((IDisposable)(*ptr)).Dispose();
        }

        A.Free(ptr);
    }
}

[DebuggerDisplay("{ToString(),raw}")]
public sealed class GC<T>:
    Ctor<T, GC<T>>,
    ConvUnscoped<object, GC<T>>,
    TryConvUnscoped<object, GC<T>>
where T: struct {
    GC() => throw new InvalidOperationException("GC<T> default constructor should never be used.");

    public static GC<T> New(T value) => Unsafe.As<GC<T>>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GC<T> From(object obj) {
        if (obj.GetType() != typeof(T)) Throw();
        return Unsafe.As<GC<T>>(obj)!;

        [DoesNotReturn, StackTraceHidden]
        static void Throw() {
            throw new InvalidCastException($"Can't cast the input object to the type GC<{typeof(T)}>");
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static GC<T> FromUnsafe(object obj) {
        return Unsafe.As<GC<T>>(obj)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFrom(object obj, [NotNullWhen(true)] out GC<T>? box) {
        if (obj.GetType() == typeof(T)) {
            box = Unsafe.As<GC<T>>(obj);
            return true;
        }

        box = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(GC<T> box) {
        return box.Ref();
    }

    public override string ToString() {
        return this.Ref().ToString()!;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) {
        return Equals(this, obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() {
        return this.Ref().GetHashCode();
    }
}

public static class BoxImpl {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Ref<T>(this GC<T> box)
    where T: struct => ref Unsafe.As<StrongBox<T>>(box).Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U As<T, U>(GC<T> box)
    where T: struct
    where U: class {
        if (default(T) is not U) Throw();

        return Unsafe.As<U>(box);

        [DoesNotReturn, StackTraceHidden]
        static void Throw() {
            throw new InvalidCastException($"The GC box of type {typeof(T)} can't be cast to the type {typeof(U)}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(this GC<T> box)
    where T: struct, IDisposable => box.Ref().Dispose();
}
