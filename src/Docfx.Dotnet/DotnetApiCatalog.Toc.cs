// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.ManagedReference;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Docfx.Dotnet;

partial class DotnetApiCatalog
{
    enum TocNodeType
    {
        None,
        Namespace,
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
        Constructor,
        Field,
        Property,
        Method,
        Event,
        Operator,
    }

    class TocNode
    {
        public string name { get; init; } = "";
        public string? href { get; init; }
        public List<TocNode>? items { get; set; }

        internal TocNodeType type;
        internal string? id;
        internal bool containsLeafNodes;
        internal List<(ISymbol symbol, Compilation compilation)> symbols = [];
    }

    private static List<TocNode> CreateToc(List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var tocNodes = new Dictionary<string, TocNode>();
        var ext = config.OutputFormat is MetadataOutputFormat.Markdown ? ".md" : ".yml";
        var toc = assemblies.SelectMany(a => CreateToc(a.symbol.GlobalNamespace, a.compilation)).ToList();

        SortToc(toc, null);

        YamlUtility.Serialize(Path.Combine(config.OutputFolder, "toc.yml"), toc, YamlMime.TableOfContent);
        return toc;

        IEnumerable<TocNode> CreateToc(ISymbol symbol, Compilation compilation)
        {
            if (!filter.IncludeApi(symbol))
                yield break;

            switch (symbol)
            {
                case INamespaceSymbol { IsGlobalNamespace: true } ns:
                    foreach (var child in ns.GetNamespaceMembers())
                        foreach (var item in CreateToc(child, compilation))
                            yield return item;
                    break;

                case INamespaceSymbol ns:
                    foreach (var item in CreateNamespaceToc(ns))
                        yield return item;
                    break;

                case INamedTypeSymbol type:
                    foreach (var item in CreateNamedTypeToc(type))
                        yield return item;
                    break;

                case IFieldSymbol or IPropertySymbol or IMethodSymbol or IEventSymbol:
                    foreach (var item in CreateMemberToc(symbol))
                        yield return item;
                    break;

                default:
                    throw new NotSupportedException($"Unknown symbol {symbol}");
            }

            IEnumerable<TocNode> CreateNamespaceToc(INamespaceSymbol ns)
            {
                var idExists = true;
                var id = VisitorHelper.PathFriendlyId(VisitorHelper.GetId(symbol));
                if (!tocNodes.TryGetValue(id, out var node))
                {
                    idExists = false;
                    tocNodes.Add(id, node = new()
                    {
                        id = id,
                        name = config.NamespaceLayout is NamespaceLayout.Nested ? symbol.Name : symbol.ToString() ?? "",
                        href = $"{id}{ext}",
                        type = TocNodeType.Namespace,
                    });
                }

                var existingNodeHasNoLeafNode = idExists && !node.containsLeafNodes;

                node.items ??= [];
                node.symbols.Add((symbol, compilation));

                foreach (var child in ns.GetNamespaceMembers())
                {
                    if (config.NamespaceLayout is NamespaceLayout.Flattened)
                        foreach (var item in CreateToc(child, compilation))
                            yield return item;
                    else if (config.NamespaceLayout is NamespaceLayout.Nested)
                        node.items.AddRange(CreateToc(child, compilation));
                }

                foreach (var child in ns.GetTypeMembers())
                {
                    node.items.AddRange(CreateToc(child, compilation));
                }

                node.containsLeafNodes = node.items.Any(i => i.containsLeafNodes);
                if (node.containsLeafNodes)
                {
                    if (!idExists || existingNodeHasNoLeafNode)
                    {
                        yield return node;
                    }
                }
            }

            IEnumerable<TocNode> CreateNamedTypeToc(INamedTypeSymbol type)
            {
                var idExists = true;
                var id = VisitorHelper.PathFriendlyId(VisitorHelper.GetId(symbol));
                if (!tocNodes.TryGetValue(id, out var node))
                {
                    idExists = false;
                    tocNodes.Add(id, node = new()
                    {
                        id = id,
                        name = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp),
                        href = $"{id}{ext}",
                        containsLeafNodes = true,
                        type = type.TypeKind switch
                        {
                            TypeKind.Class => TocNodeType.Class,
                            TypeKind.Interface => TocNodeType.Interface,
                            TypeKind.Struct => TocNodeType.Struct,
                            TypeKind.Delegate => TocNodeType.Delegate,
                            TypeKind.Enum => TocNodeType.Enum,
                            _ => throw new NotSupportedException($"Unknown type kind {type.TypeKind}"),
                        }
                    });
                }

                foreach (var child in type.GetTypeMembers())
                {
                    foreach (var item in CreateToc(child, compilation))
                        yield return item;
                }

                if (config.MemberLayout is MemberLayout.SeparatePages && type.TypeKind is TypeKind.Class or TypeKind.Interface or TypeKind.Struct)
                {
                    node.items ??= [];
                    foreach (var member in type.GetMembers())
                        node.items.AddRange(CreateToc(member, compilation));
                }

                node.symbols.Add((symbol, compilation));

                if (!idExists)
                {
                    yield return node;
                }
            }

            IEnumerable<TocNode> CreateMemberToc(ISymbol symbol)
            {
                var type = symbol switch
                {
                    IPropertySymbol => TocNodeType.Property,
                    IFieldSymbol => TocNodeType.Field,
                    IEventSymbol => TocNodeType.Event,
                    IMethodSymbol method when SymbolHelper.IsConstructor(method) => TocNodeType.Constructor,
                    IMethodSymbol method when SymbolHelper.IsMethod(method) => TocNodeType.Method,
                    IMethodSymbol method when SymbolHelper.IsOperator(method) => TocNodeType.Operator,
                    _ => TocNodeType.None,
                };

                if (type is TocNodeType.None)
                    yield break;

                var id = VisitorHelper.PathFriendlyId(VisitorHelper.GetOverloadId(symbol));
                if (!tocNodes.TryGetValue(id, out var node))
                {
                    tocNodes.Add(id, node = new()
                    {
                        id = id,
                        name = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp, overload: true),
                        href = $"{id}{ext}",
                        containsLeafNodes = true,
                        type = type,
                    });
                    yield return node;
                }

                node.symbols.Add((symbol, compilation));
            }
        }

        void SortToc(List<TocNode> items, TocNode? parentTocNode)
        {
            items.Sort((a, b) => a.type.CompareTo(b.type) is var r && r is 0 ? a.name.CompareTo(b.name) : r);

            if (parentTocNode != null)
            {
                InsertCategory(TocNodeType.Class, "Classes");
                InsertCategory(TocNodeType.Struct, "Structs");
                InsertCategory(TocNodeType.Interface, "Interfaces");
                InsertCategory(TocNodeType.Enum, "Enums");
                InsertCategory(TocNodeType.Delegate, "Delegates");
                InsertCategory(TocNodeType.Constructor, "Constructors");
                InsertCategory(TocNodeType.Field, "Fields");
                InsertCategory(TocNodeType.Property, "Properties");
                InsertCategory(TocNodeType.Method, "Methods");
                InsertCategory(TocNodeType.Event, "Events");
                InsertCategory(TocNodeType.Operator, "Operators");
            }

            foreach (var item in items)
            {
                if (item.items is not null)
                    SortToc(item.items, item);
            }

            void InsertCategory(TocNodeType type, string name)
            {
                switch (config.CategoryLayout)
                {
                    // Don't insert category.
                    case CategoryLayout.None:
                        return;

                    // Insert category as clickable TocNode.
                    case CategoryLayout.Nested:
                        {
                            // Skip when parent node is category node.
                            if (parentTocNode is { type: TocNodeType.None })
                                return;

                            // If items contains specified type node. Create new TocNode for category. and move related node to child node.
                            if (items.FirstOrDefault(i => i.type == type) is { } node)
                            {
                                var head = new TocNode { name = name, items = items.Where(x => x.type == type).ToList() };
                                items.Insert(items.IndexOf(node), head);
                                items.RemoveAll(x => x.type == type);
                            }
                            return;
                        }

                    // Insert category as text label.
                    case CategoryLayout.Flattened:
                    default:
                        {
                            if (items.FirstOrDefault(i => i.type == type) is { } node)
                                items.Insert(items.IndexOf(node), new() { name = name });
                            return;
                        }
                }
            }
        }
    }

    private static IEnumerable<(string id, List<(ISymbol symbol, Compilation compilation)> symbols)> EnumerateToc(List<TocNode> items)
    {
        foreach (var item in items)
        {
            if (item.items is not null)
                foreach (var i in EnumerateToc(item.items))
                    yield return i;

            if (item.id is not null && item.symbols.Count > 0)
                yield return (item.id, item.symbols);
        }
    }
}
