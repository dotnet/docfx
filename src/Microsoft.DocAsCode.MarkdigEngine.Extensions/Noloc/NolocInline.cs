
using Markdig.Syntax.Inlines;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class NolocInline : LeafInline
{
    public string Text { get; set; }
}
