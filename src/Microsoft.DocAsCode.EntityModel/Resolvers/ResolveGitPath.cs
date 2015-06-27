namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.DocAsCode.Utility;

    public class ResolveGitPath : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            // Moved to Metadata Visitor
            return new ParseResult(ResultLevel.Success);
        }
    }
}
