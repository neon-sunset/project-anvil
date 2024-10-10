using System.Allocators;
using System.Iter;

var numbers = (ReadOnlySpan<int>)[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

var iter = numbers
    .Where((int n) => n % 2 == 0)
    .Select((int n) => n * 2);

using var vec = NVec<int, Global>.Collect(iter);

foreach (var n in vec) {
    Console.WriteLine(n);
}
