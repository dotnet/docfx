namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax.Inlines;

    public class NolocInline : LeafInline
    {
        public string Text { get; set; }
    }
}
