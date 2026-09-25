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
        new ResolveExtensionMember(),
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
        Dictionary<string, MetadataItem> assemblyRoots = [];

        // Nesting follows the namespace, so only the dots of the namespace count. A UID that carries an
        // assembly component contributes the dots of that component as well, and they are not levels.
        var dotsPerNamespace = namespaces.ToDictionary(x => x.Key, x => VisitorHelper.TrimAssemblyUid(x.Value.Name).Count(y => y == '.'));
        foreach (var member in namespaces
            .OrderBy(x => dotsPerNamespace[x.Key])
            .Select(x => x.Value)
        )
        {
            foreach (var partialParentNamespace in GetParentNamespaces(member.Name))
            {
                if (!namespacedItems.ContainsKey(partialParentNamespace))
                {
                    var missingNamespace = new MetadataItem()
                    {
                        Type = MemberType.Namespace,
                        Name = partialParentNamespace,
                        AssemblyUid = member.AssemblyUid,
                        Items = [],
                        DisplayNames = [],
                        DisplayNamesWithType = [],
                        DisplayQualifiedNames = []
                    };
                    missingNamespace.DisplayNames.Add(SyntaxLanguage.Default, VisitorHelper.TrimAssemblyUid(partialParentNamespace));
                    namespacedItems[partialParentNamespace] = missingNamespace;
                    allReferences.TryAdd(partialParentNamespace, new());

                    Attach(missingNamespace);
                }
            }

            Attach(member);

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

        // Assembly roots have no UID to be ordered by, so they are ordered by the label they show. The
        // namespaces among them are ordered by UID again in `ToTocViewModel`, so this only decides where
        // the roots land, and it is skipped entirely when there are none.
        if (assemblyRoots.Count > 0)
        {
            root.Items = root.Items
                .OrderBy(x => x.DisplayNames?.GetLanguageProperty(SyntaxLanguage.Default) ?? x.Name, StringComparer.Ordinal)
                .ToList();
        }

        return root;

        // Attaches an item to its direct parent namespace, or to the root of the assembly it belongs to
        // when it is a top level namespace.
        void Attach(MetadataItem item)
        {
            var directParentNamespace = GetParentNamespaces(item.Name).LastOrDefault();

            var parent = directParentNamespace is not null && namespacedItems.TryGetValue(directParentNamespace, out var parentNamespace)
                ? parentNamespace
                : GetAssemblyRoot(item.AssemblyUid);

            parent.Items.Add(item);
            item.Parent = parent;
        }

        MetadataItem GetAssemblyRoot(string assemblyUid)
        {
            if (assemblyUid is null)
            {
                return root;
            }

            if (!assemblyRoots.TryGetValue(assemblyUid, out var assemblyRoot))
            {
                // This node exists to group the namespaces of one assembly and has no page of its own, so
                // it must not carry a UID: `MemberType.Toc` is what keeps `BuildMembers` from making a page
                // for it and `ToTocItemViewModel` from emitting a UID that resolves to nothing.
                assemblyRoots[assemblyUid] = assemblyRoot = new MetadataItem()
                {
                    Type = MemberType.Toc,
                    Items = [],
                    DisplayNames = new() { [SyntaxLanguage.Default] = assemblyUid },
                    DisplayNamesWithType = [],
                    DisplayQualifiedNames = [],
                    Parent = root,
                };

                root.Items.Add(assemblyRoot);
            }

            return assemblyRoot;
        }
    }

    /// <summary>
    /// Enumerates the UIDs of the namespaces containing <paramref name="originalNamespace"/>, outermost
    /// first. The assembly component is not a namespace level, so it is kept on every UID yielded instead
    /// of being split on.
    /// </summary>
    private static IEnumerable<string> GetParentNamespaces(string originalNamespace)
    {
        var namespaceName = VisitorHelper.TrimAssemblyUid(originalNamespace);
        var assemblyUid = originalNamespace[..^namespaceName.Length];

        var namespaces = namespaceName.Split('.');
        var fullNamespace = "";
        foreach (var @namespace in namespaces)
        {
            fullNamespace += $".{@namespace}";
            if (fullNamespace.TrimStart('.') != namespaceName)
                yield return assemblyUid + fullNamespace.TrimStart('.');
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
                // A namespace is never promoted out of an assembly root, even when it is the only one:
                // that root is the only thing naming the assembly. The outer root is excluded by having no
                // parent of its own, as before.
                if (metadataItem.Parent?.Type is not MemberType.Toc
                 && metadataItem.Parent?.Items.Count == 1 && metadataItem.Parent.Parent != null)
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
