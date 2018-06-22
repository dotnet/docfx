namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class TripleColonBlock : ContainerBlock
    {
        public ITripleColonExtensionInfo Extension { get; set; }
        public TripleColonBlock(BlockParser parser) : base(parser) { }
    }
}
