using System.Allocators;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using GC = System.Allocators.GC;

namespace Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 1)]
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
        var vec = new Vec<int, GC>();
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
        using var vec = new NVec<int, Alloc>();
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
[DisassemblyDiagnoser(maxDepth: 1)]
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
        using var vec = (Vec<int, GC>)[
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
    public int NVecLiteral() {
        using var vec = (NVec<int, Alloc>)[
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
