namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTableBlockToken : IMarkdownToken
    {
        public MarkdownTableBlockToken(IMarkdownRule rule, string[] header, Align[] align, string[][] cells)
        {
            Rule = rule;
            Header = header;
            Align = align;
            Cells = cells;
        }

        public IMarkdownRule Rule { get; }

        public string[] Header { get; }

        public Align[] Align { get; }

        public string[][] Cells { get; }
    }
}
