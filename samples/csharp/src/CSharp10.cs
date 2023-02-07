namespace CSharp10;

public readonly record struct ReadOnlyRecordStruct(double X, double Y, double Z);

public record struct RecordStruct(DateTime TakenAt, double Measurement);

public record class RecordClass(string FirstName, string LastName);

public struct ParameterlessStructConstructors
{
    public int X;
    public int Y { get; set; }
    public string Description { get; init; } = "Default description";

    public ParameterlessStructConstructors()
    {
        X = 10;
    }
}

public class ConstantInterpolatedStrings
{
    public const string S1 = $"Hello world";
    public const string S2 = $"Hello{" "}World";
    public const string S3 = $"{S1} Kevin, welcome to the team!";
}

public class Issue7737
{
    /// <summary>
    /// <see cref="Exception"/>
    /// </summary>
    public void Foo() { }
}
