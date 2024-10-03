using System.Diagnostics.CodeAnalysis;

namespace Anvil.Core;

public interface AsRef<T>
where T: allows ref struct {
    ref readonly T Ref { get; }
}

public interface AsUnscopedRef<T>: AsRef<T>;

public interface AsMut<T>
where T: allows ref struct {
    ref T Ref { get; }
}

public interface AsUnscopedMut<T>: AsMut<T>;

public interface Into<out T>
where T: allows ref struct {
    T Into();
}

public interface IntoUnscoped<out T>: Into<T>;

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
