// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal static class YamlMetadataResolver
{
    // Order matters
    private static readonly List<IResolverPipeline> pipelines =
    [
        new LayoutCheckAndCleanup(),
        new SetParent(),
        new ResolveReference(),
        new NormalizeSyntax(),
        new BuildMembers(),
        new SetDerivedClass(),
        new BuildToc()
    ];

    /// <summary>
    /// TODO: input Namespace list instead;
    /// TODO: Save to ...yml.map
    /// </summary>
    /// <param name="allMembers"></param>
    /// <returns></returns>
    public static MetadataModel ResolveMetadata(
        Dictionary<string, MetadataItem> allMembers,
        Dictionary<string, ReferenceItem> allReferences,
        NamespaceLayout namespaceLayout)
    {
        MetadataModel viewModel = new()
        {
            TocYamlViewModel = GenerateToc(allMembers, allReferences, namespaceLayout),
            Members = [],
        };
        ResolverContext context = new()
        {
            References = allReferences,
            Members = allMembers,
        };

        ExecutePipeline(viewModel, context);

        return viewModel;
    }

    private static MetadataItem GenerateToc(Dictionary<string, MetadataItem> allMembers, Dictionary<string, ReferenceItem> allReferences, NamespaceLayout namespaceLayout)
    {
        var namespaces = allMembers.Where(s => s.Value.Type == MemberType.Namespace);

        return namespaceLayout switch
        {
            NamespaceLayout.Flattened => GenerateFlatToc(namespaces),
            NamespaceLayout.Nested => GenerateNestedToc(namespaces, allReferences),
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

    private static MetadataItem GenerateNestedTocStructure(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces, Dictionary<string, ReferenceItem> allReferences)
    {
        var root = new MetadataItem()
        {
            Type = MemberType.Toc,
            Items = []
        };
        Dictionary<string, MetadataItem> namespacedItems = [];

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
                            Items = [],
                            DisplayNames = [],
                            DisplayNamesWithType = [],
                            DisplayQualifiedNames = []
                        };
                        missingNamespace.DisplayNames.Add(SyntaxLanguage.Default, partialParentNamespace);
                        namespacedItems[partialParentNamespace] = missingNamespace;
                        allReferences.TryAdd(partialParentNamespace, new());

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
        {
            member.Items = member.Items
                .OrderBy(x => x.Type == MemberType.Namespace ? 0 : 1)
                .ThenBy(x => x.Name)
                .ToList();

            if (member.Type == MemberType.Namespace
             && member.Items.All(x => x.Type == MemberType.Namespace))
            {
                allReferences[member.Name] = new();
            }
        }

        return root;
    }

    private static IEnumerable<string> GetParentNamespaces(string originalNamespace)
    {
        var namespaces = originalNamespace.Split('.');
        var fullNamespace = "";
        foreach (var @namespace in namespaces)
        {
            fullNamespace += $".{@namespace}";
            if (fullNamespace.TrimStart('.') != originalNamespace)
                yield return fullNamespace.TrimStart('.');
        }
    }

    private static MetadataItem GenerateNestedToc(IEnumerable<KeyValuePair<string, MetadataItem>> namespaces, Dictionary<string, ReferenceItem> allReferences)
    {
        var root = GenerateNestedTocStructure(namespaces, allReferences);

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
