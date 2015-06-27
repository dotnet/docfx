namespace Microsoft.DocAsCode.EntityModel
{
    using System.Threading.Tasks;
    using Microsoft.DocAsCode.Utility;

    public class SetParent : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    current.Parent = parent;
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }
    }
}
