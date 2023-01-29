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
            TocNamespaceStyle tocNamespaceStyle)
        {
            MetadataModel viewModel = new MetadataModel();
            viewModel.TocYamlViewModel = GenerateToc(allMembers, tocNamespaceStyle);
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

        private static MetadataItem GenerateToc(Dictionary<string, MetadataItem> allMembers, TocNamespaceStyle tocNamespaceStyle)
        {
            var namespaces = allMembers.Where(s => s.Value.Type == MemberType.Namespace);

            return tocNamespaceStyle switch
            {
                TocNamespaceStyle.Flattened => GenerateFlatToc(namespaces),
                TocNamespaceStyle.Nested => GenerateNestedToc(namespaces),
                TocNamespaceStyle.CompactNested => GenerateCompactNestedToc(namespaces),
                _ => GenerateFlatToc(namespaces),
            };
        }

        private static MetadataItem GenerateFlatToc(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces)
        {
            return new MetadataItem
            {
                Type = MemberType.Toc,
                Items = namespaces
                    .Select(x => x.Value)
                    .ToList(),
            };
        }

        private static MetadataItem GenerateNestedToc(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces)
        {
            MetadataItem root = new MetadataItem()
            {
                Type = MemberType.Toc,
                Items = new()
            };
            Dictionary<string, MetadataItem> namespacedItems = new();

            var dotsPerNamespace = namespaces.ToDictionary(x => x.Key, x => x.Value.Name.Where(y => y == '.').Count());
            foreach (var member in namespaces
                .OrderBy(x => dotsPerNamespace[x.Key])
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

        private static MetadataItem GenerateCompactNestedToc(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces)
        {
            var root = GenerateNestedToc(namespaces);

            Queue<MetadataItem> metadataItemQueue = new();
            metadataItemQueue.Enqueue(root);

            while (metadataItemQueue.TryDequeue(out var metadataItem))
            {
                if (metadataItem.Type == MemberType.Namespace)
                {
                    var lastIndex = metadataItem.Name?.LastIndexOf('.');
                    if (lastIndex >= 0)
                        metadataItem.DisplayNames.Add(SyntaxLanguage.Default, metadataItem.Name.Substring(lastIndex.Value + 1));
                }

                if (metadataItem.Items != null)
                    foreach (var item in metadataItem.Items)
                        metadataItemQueue.Enqueue(item);
            }


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
