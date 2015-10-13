namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public interface IMarkdownContext
    {
        ImmutableList<IMarkdownRule> GetRules();
    }
}
