namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class TripleColonRenderer : HtmlObjectRenderer<TripleColonBlock>
    {
        protected override void Write(HtmlRenderer renderer, TripleColonBlock b)
        {
            renderer.Write("<div").WriteAttributes(b).WriteLine(">");
            renderer.WriteChildren(b);
            renderer.WriteLine("</div>");
        }
    }
}
