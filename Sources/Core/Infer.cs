using System.Runtime.CompilerServices;

namespace System.Generics;

public struct TArgs<T> where T: allows ref struct;

public struct TArgs<T1, T2>
where T1: allows ref struct
where T2: allows ref struct;

public static partial class Infer {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TArgs<T> T<T>() where T: allows ref struct => default!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TArgs<T1, T2> T<T1, T2>()
    where T1: allows ref struct
    where T2: allows ref struct => default!;
}
