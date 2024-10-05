// // See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");

using System.Numerics;
using Anvil.Core;
using Anvil.Core.Collections;

var num = 42;
var numbox = Box.From(num);

Console.WriteLine(numbox);

for (var i = 0; i < 10_000; i++) {
    using var vec = new NVec<int, Global>();
    for (var j = 0; j < 10_000; j++) {
        vec.Add(j);
    }
    Console.WriteLine(vec[vec.Count - 1]);
}
