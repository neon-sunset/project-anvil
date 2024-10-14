using System.Runtime.CompilerServices;

namespace System.Iter;

public ref struct Take<T, U>(T iter, nuint count): Iter<U>
where T: Iter<U>, allows ref struct {
    T iter = iter;
    nuint count = count;

    public nuint? Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => iter.Count is nuint c ? Math.Min(c, count) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Next(out U item) {
        if (count > 0 && iter.Next(out item)) {
            count--;
            return true;
        }
        Unsafe.SkipInit(out item);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => iter.Dispose();
}
