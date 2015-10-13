namespace Microsoft.DocAsCode.MarkdownLite
{
    public interface IMarkdownToken
    {
        IMarkdownRule Rule { get; }
    }
}
