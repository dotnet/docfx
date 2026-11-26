namespace CSharp15;

// https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15

// Union types
public record class Cat(string Name);
public record class Dog(string Name);
public record class Bird(string Name);

public union Pet(Cat, Dog, Bird);

// Closed hierarchies
public closed record class GateState;
public record class Closed : GateState;
public record class Open(float Percent) : GateState;

// Extension indexers
public static class SequenceIndexer
{
    extension(IEnumerable<int> sequence)
    {
        public int this[int index] => sequence.ElementAt(index);
    }
}
