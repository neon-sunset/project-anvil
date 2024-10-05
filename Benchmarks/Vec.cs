using Anvil.Core;
using Anvil.Core.Collections;
using BenchmarkDotNet.Attributes;
using GC = Anvil.Core.GC;

namespace Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class VecVsList {

    public IEnumerable<int> Counts => [4, 10, 50, 100, 10_000, 1024 * 1024];

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToList(int n) {
        var list = new List<int>();
        for (var i = 0; i < n; i++) {
            list.Add(i);
        }
        return list[^1];
    }

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
        using var vec = new NVec<int, Global>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[vec.Count - 1];
    }

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToNVecJemalloc(int n) {
        using var vec = new NVec<int, Jemalloc>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[vec.Count - 1];
    }

    [Benchmark, ArgumentsSource(nameof(Counts))]
    public int AddToNVecMimalloc(int n) {
        using var vec = new NVec<int, Mimalloc>();
        for (var i = 0; i < n; i++) {
            vec.Add(i);
        }
        return vec[vec.Count - 1];
    }
}
