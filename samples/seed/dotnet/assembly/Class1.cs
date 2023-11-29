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

public unsafe struct Issue5432
{
    private fixed char Name0[30];

    public string Name
    {
        get
        {
            fixed (char* name = Name0)
            {
                return new string((char*)name);
            }
        }
    }
}
