using System.Diagnostics.CodeAnalysis;
using System.Generics;
using System.Runtime.CompilerServices;

namespace System;

public interface As<out T>
where T: allows ref struct {
    T As();
}

public interface AsUnscoped<out T>: As<T>;

public interface TryInto<T>
where T: allows ref struct {
    bool TryInto(out T value);
}

public interface TryIntoUnscoped<T>: TryInto<T>;

public interface Conv<T, out U>
where T: allows ref struct
where U: allows ref struct {
    static abstract U From(T value);
}

public interface ConvUnscoped<T, out U>: Conv<T, U>;

public interface TryConv<T, U>
where T: allows ref struct
where U: allows ref struct {
    static abstract bool TryFrom(T value, [NotNullWhen(true)] out U? result);
}

public interface TryConvUnscoped<T, U>: TryConv<T, U>;

public static class ConvImpl {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U As<T, U>(this ref T value, TArgs<U> _ = default)
    where T: struct, As<U>, allows ref struct
    where U: allows ref struct => value.As();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U AsUnscoped<T, U>(this ref T value, TArgs<U> _ = default)
    where T: struct, AsUnscoped<U>, allows ref struct => value.As();
}
