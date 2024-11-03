using System.Runtime.CompilerServices;

namespace System;

static class Disposable {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(this T value)
    where T: struct => ((IDisposable)value!).Dispose();

    public static void DisposeRange<T>(this Slice<T> values)
    where T: struct {
        foreach (var v in values) {
            v.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DisposeIfDisposable<T>(this T value)
    where T: struct {
        if (value is IDisposable) {
            ((IDisposable)value).Dispose();
        }
    }
}
