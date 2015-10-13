namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IMarkdownRule
    {
        string Name { get; }
        IMarkdownToken TryMatch(MarkdownEngine engine, ref string source);
    }
}
