namespace Microsoft.DocAsCode.EntityModel
{
    public interface IResolverPipeline
    {
        ParseResult Run(MetadataModel yaml, ResolverContext context);
    }
}
