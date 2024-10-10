using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

public static class Utils {
    public static ReadOnlySpan<U> UncheckedCast<T, U>(this ReadOnlySpan<T> span) {
        Debug.Assert(
            RuntimeHelpers.IsReferenceOrContainsReferences<T>() ==
            RuntimeHelpers.IsReferenceOrContainsReferences<U>());
        Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<U>());
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<T, U>(ref MemoryMarshal.GetReference(span)), span.Length);
    }
}
