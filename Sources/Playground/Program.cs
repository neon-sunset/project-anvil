using System.Allocators;
using System.Iter;

class Program {
    static void Main(string[] args) {
        // using var numbers = Box.New<NVec<int, Global>, Global>([
        //     0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        //     0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        //     0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        //     0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F
        // ]);

        // foreach (var n in numbers.Ref) {
        //     Console.WriteLine(n);
        // }
        var numbers = (ReadOnlySpan<int>)[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        var iter = numbers
            .Iter()
            .Select((int n) => n * 2)
            .Select((int n) => (nint)n);

        using var vec = NVec<nint, Global>.Collect(iter);

        foreach (var n in vec) {
            Console.WriteLine(n);
        }
    }
}
