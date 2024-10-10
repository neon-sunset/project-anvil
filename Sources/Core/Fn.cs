namespace System;

public interface Fn<T>
where T: allows ref struct {
    T Invoke();
}

public interface Fn<A, T>
where A: allows ref struct
where T: allows ref struct {
    T Invoke(A arg);
}

interface Fn<A1, A2, T>
where A1: allows ref struct
where A2: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2);
}

interface Fn<A1, A2, A3, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3);
}

interface Fn<A1, A2, A3, A4, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3, A4 arg4);
}

interface Fn<A1, A2, A3, A4, A5, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5);
}

interface Fn<A1, A2, A3, A4, A5, A6, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6);
}

interface Fn<A1, A2, A3, A4, A5, A6, A7, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where A7: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6, A7 arg7);
}

interface Fn<A1, A2, A3, A4, A5, A6, A7, A8, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where A7: allows ref struct
where A8: allows ref struct
where T: allows ref struct {
    T Invoke(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6, A7 arg7, A8 arg8);
}
