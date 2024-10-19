using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Anvil.Core;

static unsafe class Throw {
    [DoesNotReturn, StackTraceHidden]
    public static ref T IndexOutOfRange<T>()
    where T: allows ref struct => throw new IndexOutOfRangeException();

    [DoesNotReturn, StackTraceHidden]
    public static void* IndexOutOfRange() => throw new IndexOutOfRangeException();

    [DoesNotReturn, StackTraceHidden]
    public static void EmptySequence() => throw new InvalidOperationException("Sequence contains no elements");
}
