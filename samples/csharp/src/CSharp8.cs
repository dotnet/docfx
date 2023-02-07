using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSharp8
{
    public struct ReadOnlyMembers
    {
        public readonly int X { get; }
        public readonly int Y { get; }

        public readonly double Sum()
        {
            return X + Y;
        }

        public readonly override string ToString() => $"({X}, {Y})";

        private int counter;
        public int Counter
        {
            readonly get => counter;
            set => counter = value;
        }
    }

    public interface DefaultInterfaceMembers
    {
        public const int X = 0;
        public const double GravitationalConstant = 6.673e-11;
        private const string ProductName = "Visual C#";

        public static DefaultInterfaceMembers operator +(DefaultInterfaceMembers a) => a;

        static DefaultInterfaceMembers()
        {
            Console.WriteLine("Initializing DefaultInterfaceMembers");
        }

        public class Nested
        {
            Nested() { }
        }

        public static void DoSomething() { /*...*/ }
        public static void DoSomethingElse() { /*...*/  }

        public interface IA
        {
            void M() { }
        }
    }

    public ref struct DisposableRefStructs
    {
        public void Dispose()
        {
        }

        public static void DoSomething()
        {
            using var _ = new DisposableRefStructs();
        }
    }

    public class NullableReferenceTypes
    {
        public string? Property { get; set; }

        public string? Field;

        public static (string?, object?)? DoSomething(List<string?>? name = null) => default;
    }

    public class Misc
    {
        public static bool IsExpression(DateTime date) =>
            date is { Month: 10, Day: <= 7, DayOfWeek: DayOfWeek.Friday };

        public static bool SwitchExpression(ConsoleColor color) => color switch
        {
            ConsoleColor.White => true,
            ConsoleColor.Yellow => true,
            ConsoleColor.Red => false,
            _ => throw new ArgumentOutOfRangeException(),
        };

        public static int StaticLocalFunctions(int n)
        {
            return nthFactorial(n);

            static int nthFactorial(int number) => number < 2 
                ? 1 
                : number * nthFactorial(number - 1);
        }

        public static async Task  AsynchronousStreams()
        {
            await foreach (var number in GenerateSequence())
            {
                Console.WriteLine(number);
            }
        }

        public static async IAsyncEnumerable<int> GenerateSequence()
        {
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(100);
                yield return i;
            }
        }

        public static void UsingDeclaration()
        {
            using var stream = File.Create("foo.txt");
        }

        public static void IndicesAndRanges()
        {
            int[] numbers = new[] { 0, 10, 20, 30, 40, 50 };
            int start = 1;
            int amountToTake = 3;
            int[] subset = numbers[start..(start + amountToTake)];
        }

        public static void NullCoalescingAssignment()
        {
            string? s = null;
            s ??= "default";
        }

        public static void StackallocInNestedExpressions()
        {
            Span<int> numbers = stackalloc[] { 10, 20, 30, 40, 50, 60, 70, 80, 80, 100 };
            var index = numbers.IndexOfAny(stackalloc[] { 40, 60, 100 });
        }
    }

    /// <summary>
    /// This works: <see cref="SomeMethod"/>.
    /// This does not work: <see cref="Issue4007.SomeOtherMethod"/>.
    /// </summary>
    /// <include file='../docs.xml' path='docs/members[@name="MyTests"]/Test/*'/>
    public class Issue4007
    {
        public void SomeMethod(int a)
        {
            var x = a switch
            {
                1 => 2,
                2 => 3,
                _ => throw new ArgumentException()
            };
        }

        public void SomeOtherMethod() {}
    }
}
