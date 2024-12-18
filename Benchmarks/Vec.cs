using System.Allocators;
using System.Iter;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
//[HardwareCounters(
//    HardwareCounter.CacheMisses,
//    HardwareCounter.BranchInstructions,
//    HardwareCounter.BranchMispredictions,
//    HardwareCounter.InstructionRetired)]
public class VecAdd {

    public IEnumerable<int> Counts => [4, 10, 100, 10_000, 1024 * 1024];

    // [Benchmark, ArgumentsSource(nameof(Counts))]
    // public int AddToList(int n) {
    //     var list = new List<int>();
    //     for (var i = 0; i < n; i++) {
    //         list.Add(i);
    //     }
    //     return list[^1];
    // }

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToVecGC(int n) {
        var vec = new Vec<int, Auto>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[^1];
    }

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToVecPooled(int n) {
        using var vec = new Vec<int, Pool>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[^1];
    }

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToNVecGlobal(int n) {
        using var vec = new NVec<int, Global>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[vec.Count - 1];
    }

    //[Benchmark, ArgumentsSource(nameof(Counts))]
    //public int AddToNVecJemalloc(int n) {
    //    using var vec = new NVec<int, Jemalloc>();
    //    for (var i = 0; i < n; i++) {
    //        vec.Add(i);
    //    }
    //    return vec[vec.Count - 1];
    //}
}

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
//[HardwareCounters(
//    HardwareCounter.CacheMisses,
//    HardwareCounter.BranchInstructions,
//    HardwareCounter.BranchMispredictions,
//    HardwareCounter.InstructionRetired)]
public class VecFromRVA {
    [Benchmark]
    public int ListLiteral() {
        var list = (List<int>)[
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
        ];

        var res = list[^1];
        foreach (var n in list) res += n;
        return res;
    }

    [Benchmark]
    public int VecGCLiteral() {
        var vec = (Vec<int, Auto>)[
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
        ];

        var res = vec[^1];
        foreach (var n in vec) res += n;
        return res;
    }

    [Benchmark]
    public int VecPoolLiteral() {
        using var vec = (Vec<int, Pool>)[
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
        ];

        var res = vec[^1];
        foreach (var n in vec) res += n;
        return res;
    }

    [Benchmark]
    public int NVecLiteralGlobal() {
        using var vec = (NVec<int, Global>)[
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
        ];
        var res = vec[vec.Count - 1];
        foreach (var n in vec) res += n;
        return res;
    }
}

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2)]
public class VecComplex {
    [Benchmark]
    public int TwoListsAndBoxes() {
        var sum = 0;
        var list = (List<int>)[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var vec = list
            .Where(n => n % 2 == 0)
            .Select((int n) => n * 2)
            .ToList();
        foreach (var n in vec) {
            sum += n;
        }

        var boxes = vec
            .Select((int n) => Box.GC(n))
            .ToList();

        foreach (var box in boxes) {
            sum += box;
        }
        return sum;
    }

    [Benchmark]
    public int TwoVecsAndBoxes() {
        var sum = 0;
        var numbers = (ReadOnlySpan<int>)[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        using var vec = numbers
            .Where(n => n % 2 == 0)
            .Collect((int n) => n * 2);

        foreach (var n in vec) {
            sum += n;
        }

        using var boxes = vec
            .Iter()
            .Collect((int n) => Box.New(n));

        foreach (var box in boxes) {
            sum += box;
        }

        return sum;
    }
}
