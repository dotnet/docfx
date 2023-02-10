using System.ComponentModel;

namespace BuildFromAssembly;

/// <summary>
/// This is a test class.
/// </summary>
public class Class1
{
    public static void HelloWorld() { }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HiddenAPI() { }
}
