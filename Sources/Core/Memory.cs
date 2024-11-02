using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System;

static class Memory {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear<T>(ref T ptr, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref ptr, (int)(uint)size)
                .Clear();
            ptr = ref Unsafe.Add(ref ptr, size);
            count -= size;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Copy<T>(T* src, T* dst, nuint count)
    where T: unmanaged {
        var length = count * (nuint)sizeof(T);
        Buffer.MemoryCopy(src, dst, length, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill<T>(ref T ptr, T value, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref ptr, (int)(uint)size)
                .Fill(value);
            ptr = ref Unsafe.Add(ref ptr, size);
            count -= size;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint? IndexOf<T>(ref T ptr, nuint length, T value)
    where T: IEquatable<T> {
        var offset = (nuint)0;
        while (length > 0) {
            var size = Math.Min(length, int.MaxValue);
            var span = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.Add(ref ptr, offset), (int)(uint)size);
            var index = span.IndexOf(value);
            if (index > -1) return offset + (uint)index;

            offset += size;
            length -= size;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint? IndexOfUnconstrained<T>(ref T ptr, nuint length, T value) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static nuint? IndexOfEquatable<U>(ref T ptr, nuint length, T value)
        where U: IEquatable<U> {
            return IndexOf(
                ref Unsafe.As<T, U>(ref ptr),
                length,
                Unsafe.As<T, U>(ref value));
        }

        var type = typeof(T);
        if (type.IsValueType && type.IsEnum) {
            type = type.GetEnumUnderlyingType();
        }

        if (type == typeof(byte)) return IndexOfEquatable<byte>(ref ptr, length, value);
        if (type == typeof(sbyte)) return IndexOfEquatable<sbyte>(ref ptr, length, value);
        if (type == typeof(short)) return IndexOfEquatable<short>(ref ptr, length, value);
        if (type == typeof(ushort)) return IndexOfEquatable<ushort>(ref ptr, length, value);
        if (type == typeof(int)) return IndexOfEquatable<int>(ref ptr, length, value);
        if (type == typeof(uint)) return IndexOfEquatable<uint>(ref ptr, length, value);
        if (type == typeof(long)) return IndexOfEquatable<long>(ref ptr, length, value);
        if (type == typeof(ulong)) return IndexOfEquatable<ulong>(ref ptr, length, value);
        if (type == typeof(nint)) return IndexOfEquatable<nint>(ref ptr, length, value);
        if (type == typeof(nuint)) return IndexOfEquatable<nuint>(ref ptr, length, value);
        if (type == typeof(float)) return IndexOfEquatable<float>(ref ptr, length, value);
        if (type == typeof(double)) return IndexOfEquatable<double>(ref ptr, length, value);
        if (type == typeof(decimal)) return IndexOfEquatable<decimal>(ref ptr, length, value);
        if (type == typeof(char)) return IndexOfEquatable<char>(ref ptr, length, value);
        if (type == typeof(bool)) return IndexOfEquatable<bool>(ref ptr, length, value);

        for (nuint i = 0; i < length; i++) {
            var item = Unsafe.Add(ref ptr, i);
            if (EqualityComparer<T>.Default.Equals(item, value)) {
                return i;
            }
        }

        return null;
    }
}

public ref struct RefEnumerator<T>: IEnumerator<T> {
    ref T ptr;
    readonly ref T end;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefEnumerator(ReadOnlySpan<T> span) {
        ptr = ref MemoryMarshal.GetReference(span);
        end = ref Unsafe.Add(ref ptr, span.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RefEnumerator(ref T start, nuint length) {
        ptr = ref start;
        end = ref Unsafe.Add(ref start, length);
    }

    public T Current { readonly get; private set; } = default!;

    readonly object IEnumerator.Current => Current!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() {
        if (Unsafe.IsAddressLessThan(ref ptr, ref end)) {
            Current = ptr;
            ptr = ref Unsafe.Add(ref ptr, 1);
            return true;
        }
        return false;
    }

    readonly void IDisposable.Dispose() { }
    readonly void IEnumerator.Reset() => throw new NotSupportedException();
}

public unsafe struct PtrEnumerator<T>: IEnumerator<T>
where T: unmanaged {
    T* ptr;
    readonly T* end;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PtrEnumerator(T* start, nuint length) {
        ptr = start;
        end = start + length;
    }

    public T Current { readonly get; private set; }
    readonly object IEnumerator.Current => throw new NotSupportedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext() {
        var curr = ptr;
        if (curr < end) {
            Current = *curr;
            ptr = curr + 1;
            return true;
        }
        return false;
    }

    readonly void IDisposable.Dispose() { }
    readonly void IEnumerator.Reset() => throw new NotSupportedException();
}
