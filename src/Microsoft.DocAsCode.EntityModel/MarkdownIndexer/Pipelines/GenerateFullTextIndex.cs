namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public class GenerateFullTextIndex : IIndexerPipeline
    {
        public ParseResult Run(MapFileItemViewModel item, IndexerContext context)
        {
            return new ParseResult(ResultLevel.Success);
        }
    }
}
