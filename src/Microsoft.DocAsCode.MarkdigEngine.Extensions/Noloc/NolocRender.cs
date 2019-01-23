namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class NolocRender : HtmlObjectRenderer<NolocInline>
    {
        protected override void Write(HtmlRenderer renderer, NolocInline obj)
        {
            renderer.Write(obj.Text);
        }
    }
}
