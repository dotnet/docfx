using System.ComponentModel;

namespace BuildFromAssembly;

/// <summary>
/// This is a test class.
/// </summary>
public class Class1
{
    /// <summary>
    /// HelloWorld method.
    /// </summary>
    public static void HelloWorld() { }

    /// <summary>
    /// HiddenAPI method.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HiddenAPI() { }
}
