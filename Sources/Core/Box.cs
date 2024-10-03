// TODO: Add MIT license attribution to CommunityToolkit.HighPerformance

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Anvil.Core;

public static class Box {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T> From<T>(T value)
    where T: struct => Unsafe.As<Box<T>>(value);

    [return: NotNullIfNotNull(nameof(option))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T>? From<T>(T? option)
    where T: struct => option.HasValue ? Unsafe.As<Box<T>>(option.Value) : null;
}

[DebuggerDisplay("{ToString(),raw}")]
public sealed class Box<T>:
    AsUnscopedMut<T>,
    IntoUnscoped<T>,
    Into<Span<T>>,
    Ctor<T, Box<T>>,
    ConvUnscoped<object, Box<T>>,
    TryConvUnscoped<object, Box<T>>
where T : struct {
    Box() => throw new InvalidOperationException("Box<T> default constructor should never be used.");

    public ref T Ref => ref this.AsMut();

    public T Into() => this;

    public static Box<T> New(T value) => Unsafe.As<Box<T>>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Box<T> From(object obj) {
        if (obj.GetType() != typeof(T)) Throw();
        return Unsafe.As<Box<T>>(obj)!;

        [DoesNotReturn, StackTraceHidden]
        static void Throw() {
            throw new InvalidCastException($"Can't cast the input object to the type Box<{typeof(T)}>");
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Box<T> FromUnsafe(object obj) {
        return Unsafe.As<Box<T>>(obj)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFrom(object obj, [NotNullWhen(true)] out Box<T>? box) {
        if (obj.GetType() == typeof(T)) {
            box = Unsafe.As<Box<T>>(obj);
            return true;
        }

        box = null;
        return false;
    }

    public U Into<U>()
    where U: class => BoxImpl.Into<T, U>(this);

    Span<T> Into<Span<T>>.Into() => new(ref this.AsMut());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(Box<T> box) {
        return Unsafe.As<StrongBox<T>>(box).Value;
    }

    public override string ToString() {
        return this.AsMut().ToString()!;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) {
        return Equals(this, obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode() {
        return this.AsMut().GetHashCode();
    }
}

file static class BoxImpl {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsMut<T>(this Box<T> box)
    where T: struct {
        return ref Unsafe.Unbox<T>(box);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref readonly T AsRef<T>(this Box<T> box)
    where T: struct {
        return ref Unsafe.Unbox<T>(box);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U Into<T, U>(Box<T> box)
    where T: struct
    where U: class {
        if (default(T) is not U) Throw();

        return Unsafe.As<U>(box);

        [DoesNotReturn, StackTraceHidden]
        static void Throw() {
            throw new InvalidCastException($"The box of type {typeof(T)} can't be cast to the type {typeof(U)}");
        }
    }
}
