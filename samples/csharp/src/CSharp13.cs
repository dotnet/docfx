namespace CSharp13;

// https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13#allows-ref-struct
public class AllowRefStruct<T>
    where T : allows ref struct
{
    // Use T as a ref struct:
    public void Method(scoped T p)
    {
        // The parameter p must follow ref safety rules
    }
}
