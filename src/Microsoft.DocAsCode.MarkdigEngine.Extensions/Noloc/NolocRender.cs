
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class NolocRender : HtmlObjectRenderer<NolocInline>
{
    protected override void Write(HtmlRenderer renderer, NolocInline obj)
    {
        renderer.Write(obj.Text);
    }
}
