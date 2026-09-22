using Microsoft.AspNetCore.Components;

namespace Compatibility.Razor;

/// <summary>A component base with an overridable API.</summary>
public class WidgetBase : ComponentBase
{
    /// <summary>Describes the component.</summary>
    public virtual string Describe() => "Base component.";
}
