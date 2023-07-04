using System.ComponentModel;

namespace BuildFromAssembly;

/// <summary>
/// This is a test class.
/// </summary>
public class Class1
{
    /// <summary>
    /// HelloWorld.
    /// </summary>
    public static void HelloWorld() { }

    /// <summary>
    /// HiddenAPI.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void HiddenAPI() { }
}
