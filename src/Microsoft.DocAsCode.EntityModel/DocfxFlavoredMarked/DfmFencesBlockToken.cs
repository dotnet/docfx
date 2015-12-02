namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;

    public class DfmFencesBlockToken : IMarkdownToken
    {
        public DfmFencesBlockToken(IMarkdownRule rule, string name, string path, string lang = null, string title = null)
        {
            Rule = rule;
            Path = path;
            Lang = lang;
            Name = name;
            Title = title;
        }

        public IMarkdownRule Rule { get; }

        public string Path { get; }

        public string Lang { get; }

        public string Name { get; }

        public string Title { get; }

        public string SourceMarkdown { get; set; }
    }
}
