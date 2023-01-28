// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public static class YamlMetadataResolver
    {
        // Order matters
        private static readonly List<IResolverPipeline> pipelines = new List<IResolverPipeline>
        {
            new LayoutCheckAndCleanup(),
            new SetParent(),
            new CopyInherited(),
            new ResolveReference(),
            new NormalizeSyntax(),
            new BuildMembers(),
            new SetDerivedClass(),
            new BuildToc()
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
            bool preserveRawInlineComments,
            bool useMultiLevelToc)
        {
            MetadataModel viewModel = new MetadataModel();
            viewModel.TocYamlViewModel = GenerateToc(allMembers, useMultiLevelToc);
            viewModel.Members = new List<MetadataItem>();
            ResolverContext context = new ResolverContext
            {
                References = allReferences,
                Members = allMembers,
                PreserveRawInlineComments = preserveRawInlineComments,
            };

            ExecutePipeline(viewModel, context);

            return viewModel;
        }

        private static MetadataItem GenerateToc(Dictionary<string, MetadataItem> allMembers, bool useMultiLevelToc)
        {
            var namespaces = allMembers.Where(s => s.Value.Type == MemberType.Namespace);

            if (!useMultiLevelToc)
                return new MetadataItem
                {
                    Type = MemberType.Toc,
                    Items = namespaces
                        .Select(x => x.Value)
                        .OrderBy(x => x.Name)
                        .ToList(),
                };


            MetadataItem root = new MetadataItem()
            {
                Type = MemberType.Toc,
                Items = new()
            };
            Dictionary<string, MetadataItem> namespacedItems = new();

            foreach (var member in namespaces
                .OrderBy(x => x.Value.Name.Where(x => x == '.').Count())
                .Select(x => x.Value)
            )
            {
                namespacedItems[member.Name] = member;

                if (member.Name.Contains('.'))
                {
                    var parentNamespace = member.Name.Substring(0, member.Name.LastIndexOf('.'));
                    if (namespacedItems.TryGetValue(parentNamespace, out var parent))
                        parent.Items.Add(member);
                    else
                        root.Items.Add(member);
                }
                else
                    root.Items.Add(member);
            }

            foreach (var member in namespacedItems.Values)
                member.Items = member.Items
                    .OrderBy(x => x.Type == MemberType.Namespace ? 0 : 1)
                    .ThenBy(x => x.Name)
                    .ToList();

            return root;
        }

        public static void ExecutePipeline(MetadataModel yaml, ResolverContext context)
        {
            foreach (var pipeline in pipelines)
            {
                pipeline.Run(yaml, context);
            }
        }
    }
}
