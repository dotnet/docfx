
using System.ComponentModel;

namespace Docfx.Plugins;

public class PropertyChangedEventArgs<T>
    : PropertyChangedEventArgs
{
    public PropertyChangedEventArgs(string propertyName, T original, T current)
        : base(propertyName)
    {
        Original = original;
        Current = current;
    }

    public T Original { get; }

    public T Current { get; }
}
