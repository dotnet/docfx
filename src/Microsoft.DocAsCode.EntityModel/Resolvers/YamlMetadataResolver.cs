namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;
    using System.Linq;

    public static class YamlMetadataResolver
    {
        // Order matters
        private static readonly List<IResolverPipeline> pipelines = new List<IResolverPipeline>()
        {
            new LayoutCheckAndCleanup(),
            new SetParent(),
            new ResolveRelativePath(),
            new BuildIndex(),
            new ResolveLink(),
            new ResolveGitPath(),
            new ResolveReference(),
            new ResolvePath(),
            new NormalizeSyntax(),
            new BuildMembers(),
            new BuildToc(),
        };

        /// <summary>
        /// TODO: input Namespace list instead; 
        /// TODO: Save to ...yml.map
        /// </summary>
        /// <param name="allMembers"></param>
        /// <returns></returns>
        public static MetadataModel ResolveMetadata(
            Dictionary<string, MetadataItem> allMembers,
            Dictionary<string, ReferenceItem> allReferences,
            string apiFolder)
        {
            MetadataModel viewModel = new MetadataModel();
            viewModel.Indexer = new ApiReferenceModel();
            viewModel.TocYamlViewModel = new MetadataItem()
            {
                Type = MemberType.Toc,
                Items = allMembers.Where(s => s.Value.Type == MemberType.Namespace).Select(s => s.Value).ToList(),
            };
            viewModel.Members = new List<MetadataItem>();
            ResolverContext context = new ResolverContext
            {
                ApiFolder = apiFolder,
                References = allReferences,
            };
            var result = ExecutePipeline(viewModel, context);

            result.WriteToConsole();
            return viewModel;
        }

        public static ParseResult ExecutePipeline(MetadataModel yaml, ResolverContext context)
        {
            ParseResult result = new ParseResult(ResultLevel.Success);
            foreach(var pipeline in pipelines)
            {
                result = pipeline.Run(yaml, context);
                if (result.ResultLevel == ResultLevel.Error)
                {
                    return result;
                }

                if (!string.IsNullOrEmpty(result.Message))
                {
                    result.WriteToConsole();
                }
            }

            return result;
        }
    }
}
