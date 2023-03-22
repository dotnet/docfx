// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.DataContracts.ManagedReference;

namespace Microsoft.DocAsCode.Dotnet;

internal static class YamlMetadataResolver
{
    // Order matters
    private static readonly List<IResolverPipeline> pipelines = new()
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
        NamespaceLayout namespaceLayout)
    {
        MetadataModel viewModel = new();
        viewModel.TocYamlViewModel = GenerateToc(allMembers, namespaceLayout);
        viewModel.Members = new List<MetadataItem>();
        ResolverContext context = new()
        {
            References = allReferences,
            Members = allMembers,
            PreserveRawInlineComments = preserveRawInlineComments,
        };

        ExecutePipeline(viewModel, context);

        return viewModel;
    }

    private static MetadataItem GenerateToc(Dictionary<string, MetadataItem> allMembers, NamespaceLayout namespaceLayout)
    {
        var namespaces = allMembers.Where(s => s.Value.Type == MemberType.Namespace);

        return namespaceLayout switch
        {
            NamespaceLayout.Flattened => GenerateFlatToc(namespaces),
            NamespaceLayout.Nested => GenerateNestedToc(namespaces),
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

    private static MetadataItem GenerateNestedTocStructure(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces)
    {
        var root = new MetadataItem()
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
            if (member.Name.Contains('.'))
            {
                var parents = GetParentNamespaces(member.Name);
                foreach (var partialParentNamespace in parents)
                {
                    if (!namespacedItems.ContainsKey(partialParentNamespace))
                    {
                        var missingNamespace = new MetadataItem()
                        {
                            Type = MemberType.Namespace,
                            Name = partialParentNamespace,
                            Items = new(),
                            DisplayNames = new(),
                            DisplayNamesWithType = new(),
                            DisplayQualifiedNames = new()
                        };
                        namespacedItems[partialParentNamespace] = missingNamespace;

                        if (!partialParentNamespace.Contains('.'))
                        {
                            root.Items.Add(missingNamespace);
                            missingNamespace.Parent = root;
                        }
                        else
                        {
                            var parentNamespace = namespacedItems[partialParentNamespace.Substring(0, partialParentNamespace.LastIndexOf('.'))];
                            missingNamespace.Parent = parentNamespace;
                            parentNamespace.Items.Add(missingNamespace);                                    
                        }
                    }
                }

                var directParentNamespace = parents.Last();
                if (namespacedItems.TryGetValue(directParentNamespace, out var parent))
                {
                    parent.Items.Add(member);
                    member.Parent = parent;
                }
                else
                {
                    root.Items.Add(member);
                    member.Parent = root;
                }
            }
            else
                root.Items.Add(member);

            namespacedItems[member.Name] = member;
        }

        foreach (var member in namespacedItems.Values)
            member.Items = member.Items
                .OrderBy(x => x.Type == MemberType.Namespace ? 0 : 1)
                .ThenBy(x => x.Name)
                .ToList();

        return root;
    }

    private static IEnumerable<string> GetParentNamespaces(string originalNamespace)
    {
        var namespaces = originalNamespace.Split(".");
        var fullNamespace = "";
        foreach (var @namespace in namespaces)
        {
            fullNamespace += $".{@namespace}";
            if (fullNamespace.TrimStart('.') != originalNamespace)
                yield return fullNamespace.TrimStart('.');
        }
    }

    private static MetadataItem GenerateNestedToc(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces)
    {
        var root = GenerateNestedTocStructure(namespaces);

        Queue<MetadataItem> metadataItemQueue = new();
        metadataItemQueue.Enqueue(root);

        while (metadataItemQueue.TryDequeue(out var metadataItem))
        {
            if (metadataItem.Type == MemberType.Namespace)
            {
                if (metadataItem.Parent?.Items.Count == 1 && metadataItem.Parent.Parent != null)
                {
                    metadataItem.Parent.Parent.Items.Add(metadataItem);
                    metadataItem.Parent.Parent.Items.Remove(metadataItem.Parent);
                    metadataItem.Parent = metadataItem.Parent.Parent;
                }

                var lastIndex = metadataItem.Name?.LastIndexOf('.');
                if (metadataItem.Parent != root)
                {
                    if (metadataItem.Parent?.Name != null && metadataItem.Name.StartsWith(metadataItem.Parent.Name))
                        metadataItem.DisplayNames.Add(SyntaxLanguage.Default, metadataItem.Name.Substring(metadataItem.Parent.Name.Length + 1));
                    else if (lastIndex >= 0 && metadataItem.Parent != root)
                        metadataItem.DisplayNames.Add(SyntaxLanguage.Default, metadataItem.Name.Substring(lastIndex.Value + 1));
                } else if (!metadataItem.DisplayNames.ContainsKey(SyntaxLanguage.Default))
                    metadataItem.DisplayNames.Add(SyntaxLanguage.Default, metadataItem.Name);
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
