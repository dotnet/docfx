namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class SplitToken
    {
        public IMarkdownToken Token { get; set; }

        public List<IMarkdownToken> InnerTokens { get; set; }

        public SplitToken(IMarkdownToken token)
        {
            Token = token;
            InnerTokens = new List<IMarkdownToken>();
        }
    }
}
