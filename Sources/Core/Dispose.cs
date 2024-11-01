using System.Runtime.CompilerServices;

namespace System;

static class Disposable {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispose<T>(this T value) => ((IDisposable)value!).Dispose();

    public static void DisposeRange<T>(this Slice<T> values) {
        foreach (var v in values) {
            v.Dispose();
        }
    }
}
