namespace System;

interface Ctor<T>
where T: new(), allows ref struct {
    static abstract T New();
}

interface Ctor<A, T>
where A: allows ref struct
where T: allows ref struct {
    static abstract T New(A arg);
}

interface Ctor<A1, A2, T>
where A1: allows ref struct
where A2: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2);
}

interface Ctor<A1, A2, A3, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3);
}

interface Ctor<A1, A2, A3, A4, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3, A4 arg4);
}

interface Ctor<A1, A2, A3, A4, A5, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5);
}

interface Ctor<A1, A2, A3, A4, A5, A6, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6);
}

interface Ctor<A1, A2, A3, A4, A5, A6, A7, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where A7: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6, A7 arg7);
}

interface Ctor<A1, A2, A3, A4, A5, A6, A7, A8, T>
where A1: allows ref struct
where A2: allows ref struct
where A3: allows ref struct
where A4: allows ref struct
where A5: allows ref struct
where A6: allows ref struct
where A7: allows ref struct
where A8: allows ref struct
where T: allows ref struct {
    static abstract T New(A1 arg1, A2 arg2, A3 arg3, A4 arg4, A5 arg5, A6 arg6, A7 arg7, A8 arg8);
}
