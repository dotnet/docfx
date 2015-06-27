namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    public class GenerateFullTextIndex : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            return new ParseResult(ResultLevel.Success);
        }
    }
}
