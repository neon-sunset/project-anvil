using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Anvil.Core;

static class Memory {
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

    public static void Copy<T>(ref T src, ref T dst, nuint count) {
        while (count > 0) {
            var size = Math.Min(count, int.MaxValue);
            MemoryMarshal
                .CreateSpan(ref src, (int)(uint)size)
                .CopyTo(MemoryMarshal.CreateSpan(ref dst, (int)(uint)size));
            src = ref Unsafe.Add(ref src, size);
            dst = ref Unsafe.Add(ref dst, size);
            count -= size;
        }
    }

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
}
