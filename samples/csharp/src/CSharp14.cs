namespace CSharp14;

// https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#the-field-keyword
public class FieldBackedPropertySample
{
    public int FieldBackedProperty
    {
        get;
        set => field = value;
    }
}

public class SampleClass
{
    public required string Value { get; init; }
}

public static class SampleClassExtensions
{
    // Extension block
    extension(SampleClass sample)
    {
        // Extension property:
        public string ExtensionProperty => sample.Value;

        // Extension method:
        public void ExtensionMethod()
        {
        }
    }

    // Extension block for static type
    extension(SampleClass)
    {
        // static extension method:
        public static void StaticExtensionMethod()
        {
        }

        // static extension property:
        public static int StaticExtensionProperty => default;

        // static user defined operator:
        public static SampleClass operator +(SampleClass value1, SampleClass value2)
            => new() { Value = value1.Value + value2.Value };
    }
}

public class UserDefinedCompoundAssignmentOperatorsSample
{
    public int? Value { get; set; }

    public override string? ToString() => Value?.ToString();

    public void operator ++() { Value += 1; }

    public void operator --() { Value -= 1; }

    public void operator +=(UserDefinedCompoundAssignmentOperatorsSample c) { Value += c?.Value; }

    public void operator -=(UserDefinedCompoundAssignmentOperatorsSample c) { Value -= c?.Value; }

    public void operator *=(UserDefinedCompoundAssignmentOperatorsSample c) { Value *= c?.Value; }

    public void operator /=(UserDefinedCompoundAssignmentOperatorsSample c) { Value /= c?.Value; }

    public void operator %=(UserDefinedCompoundAssignmentOperatorsSample c) { Value %= c?.Value; }

    public void operator &=(UserDefinedCompoundAssignmentOperatorsSample c) { Value &= c?.Value; }

    public void operator |=(UserDefinedCompoundAssignmentOperatorsSample c) { Value |= c?.Value; }

    public void operator ^=(UserDefinedCompoundAssignmentOperatorsSample c) { Value ^= c?.Value; }

    public void operator <<=(int shift) { Value <<= shift; }

    public void operator >>=(int shift) { Value >>= shift; }
}
