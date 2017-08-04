namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class SplitToken : IMarkdownToken
    {
        public IMarkdownToken Token { get; set; }

        public List<IMarkdownToken> InnerTokens { get; set; }

        public SplitToken(IMarkdownToken token)
        {
            Token = token;
            InnerTokens = new List<IMarkdownToken>();
            Rule = token.Rule;
            Context = token.Context;
            SourceInfo = token.SourceInfo;
        }

        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public SourceInfo SourceInfo { get; }
    }

    public class DfmSectionBlockSplitToken : SplitToken
    {
        public DfmSectionBlockSplitToken(IMarkdownToken token) : base(token) { }
    }

    public class DfmNoteBlockSplitToken : SplitToken
    {
        public DfmNoteBlockSplitToken(IMarkdownToken token) : base(token) { }
    }

    public class DfmVideoBlockSplitToken : SplitToken
    {
        public DfmVideoBlockSplitToken(IMarkdownToken token) : base(token) { }
    }

    public class DfmDefaultBlockQuoteBlockSplitToken : SplitToken
    {
        public DfmDefaultBlockQuoteBlockSplitToken(IMarkdownToken token) : base(token) { }
    }
}
