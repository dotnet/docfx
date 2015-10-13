namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public abstract class MarkdownBlockContext : IMarkdownContext
    {
        protected MarkdownBlockContext(IMarkdownContext inlineContext)
        {
            InlineContext = inlineContext;
        }

        internal IMarkdownContext InlineContext { get; }

        public abstract ImmutableList<IMarkdownRule> GetRules();

        public IMarkdownContext GetInlineContext() => InlineContext;

        public abstract IMarkdownContext GetNonTopBlockContext();
    }
}
