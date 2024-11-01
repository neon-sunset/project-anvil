using System.Iter;

var numbers = (ReadOnlySpan<int>)[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

using var vec = numbers
    .Where(n => n % 2 == 0)
    .Collect((int n) => n * 2);

foreach (var n in vec) {
    Console.WriteLine(n);
}

// Exiting the scope correctly frees both the memory allocated
// for the vec and the memory allocated for the boxes.
using var boxes = vec
    .Iter()
    .Collect((int n) => Box.New(n));

foreach (var box in boxes) {
    Console.WriteLine(box.ToString());
}
