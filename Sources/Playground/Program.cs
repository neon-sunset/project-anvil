using Anvil.Core;
using Anvil.Core.Collections;

var num = 42;
var numbox = Box.From(num);

Console.WriteLine(numbox);

var res = 0;
for (var i = 0; i < 100_000; i++) {
    var vec = new NVec<int, Jemalloc>();
    for (var j = 0; j < 10_000; j++) {
        vec.Add(j);
    }
    res = vec[vec.Count - 1];
    vec.Dispose();
}
Console.WriteLine(res);
