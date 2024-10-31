using System.Runtime.CompilerServices;

namespace System.Iter;

public static partial class Ops {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T, U>(this T iter, U value)
    where T: Iterator<U>, allows ref struct
    where U: IEquatable<U>, allows ref struct {
        using (iter) {
            while (iter.Next(out var item)) {
                if (item.Equals(value)) {
                    return true;
                }
            }
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains<T, U, C>(this T iter, U value, C comparer)
    where T: Iterator<U>, allows ref struct
    where U: allows ref struct
    where C: IEqualityComparer<U> {
        using (iter) {
            while (iter.Next(out var item)) {
                if (comparer.Equals(item, value)) {
                    return true;
                }
            }
            return false;
        }
    }
}
