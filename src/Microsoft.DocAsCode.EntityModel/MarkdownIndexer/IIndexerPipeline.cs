namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    public interface IIndexerPipeline
    {
        ParseResult Run(MapFileItemViewModel item, IndexerContext context);
    }
}
