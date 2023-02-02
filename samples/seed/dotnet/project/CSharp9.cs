using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSharp9
{
    public class RecordTypes
    {
        public record Person(string FirstName, string LastName);
        
        public record Teacher(string FirstName, string LastName, int Grade) : Person(FirstName, LastName);
    }

    public class InitOnlySetters
    {
        public DateTime RecordedAt { get; init; }
        public decimal TemperatureInCelsius { get; init; }
        public decimal PressureInMillibars { get; init; }
    }

    public class NativeSizedIntegers
    {
        public nint X;
        public nuint Y;        
    }

    public unsafe class FunctionPointers
    {
        public static void Example(
            Action<int> a,
            delegate*<int, void> b,
            delegate* managed<int, int> c,
            delegate* unmanaged<int, int> d,
            delegate* unmanaged[Cdecl] <int, int> e,
            delegate* unmanaged[Stdcall, SuppressGCTransition] <int, int> f)
        {
            a(42);
            b(42);
            c(42);
            d(42);
            e(42);
            f(42);
        }
    }
}
