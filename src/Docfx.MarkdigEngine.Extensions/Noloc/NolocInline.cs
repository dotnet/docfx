
using Markdig.Syntax.Inlines;

namespace Docfx.MarkdigEngine.Extensions;

public class NolocInline : LeafInline
{
    public string Text { get; set; }
}
