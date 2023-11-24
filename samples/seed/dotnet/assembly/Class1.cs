using System.ComponentModel;

namespace BuildFromAssembly;

/// <summary>
/// This is a test class.
/// </summary>
public class Class1
{
    /// <summary>
    /// Hello World.
    /// </summary>
    public static void HelloWorld() { }

    /// <summary>
    /// Hidden API.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HiddenAPI() { }
}
