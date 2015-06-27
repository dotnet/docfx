namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    public interface IIndexerPipeline
    {
        ParseResult Run(MapFileItemViewModel item, IndexerContext context);
    }
}
