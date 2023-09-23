// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

#nullable enable

namespace Docfx.Dotnet;

static class MarkdownFormatter
{
    enum TocNodeType
    {
        Namespace,
        Class,
        Struct,
        Interface,
        Enum,
        Delegate,
    }

    class TocNode
    {
        public string name { get; init; } = "";
        public string? href { get; init; }
        public List<TocNode>? items { get; set; }

        internal TocNodeType type;
        internal string? id;
        internal bool containsLeafNodes;
        internal List<(ISymbol symbol, Compilation compilation)> symbols = new();
    }

    public static void Save(List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Logger.LogWarning($"Markdown output format is experimental.");

        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.symbol.FindExtensionMethods(filter)).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.symbol), SymbolEqualityComparer.Default);

        var tocNodes = new Dictionary<string, TocNode>();
        var allSymbols = new List<(ISymbol symbol, Compilation compilation)>();
        var toc = assemblies.SelectMany(a => CreateToc(a.symbol.GlobalNamespace, a.compilation)).ToList();

        allSymbols.Sort((a, b) => a.symbol.Name.CompareTo(b.symbol.Name));
        SortToc(toc, root: true);

        YamlUtility.Serialize(Path.Combine(config.OutputFolder, "toc.yml"), toc, YamlMime.TableOfContent);
        Parallel.ForEach(EnumerateToc(toc), n => SaveTocNode(n.id, n.symbols));

        Logger.LogInfo($"Export succeed: {EnumerateToc(toc).Count()} items");

        IEnumerable<TocNode> CreateToc(ISymbol symbol, Compilation compilation)
        {
            if (!filter.IncludeApi(symbol))
                yield break;

            var id = VisitorHelper.GetId(symbol);

            switch (symbol)
            {
                case INamespaceSymbol ns when ns.IsGlobalNamespace:
                    foreach (var child in ns.GetNamespaceMembers())
                    {
                        foreach (var item in CreateToc(child, compilation))
                            yield return item;
                    }
                    break;

                case INamespaceSymbol ns:
                    var idExists = true;
                    if (!tocNodes.TryGetValue(id, out var node))
                    {
                        idExists = false;
                        tocNodes.Add(id, node = new()
                        {
                            id = id,
                            name = config.NamespaceLayout switch
                            {
                                NamespaceLayout.Nested => symbol.Name,
                                NamespaceLayout.Flattened => symbol.ToString() ?? "",
                            },
                            href = $"{id}.md",
                            type = TocNodeType.Namespace,
                        });
                    }

                    node.items ??= new();
                    node.symbols.Add((symbol, compilation));
                    allSymbols.Add((symbol, compilation));

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
                    if (!idExists && node.containsLeafNodes)
                    {
                        yield return node;
                    }
                    break;

                case INamedTypeSymbol type:
                    if (!tocNodes.TryGetValue(id, out node))
                    {
                        tocNodes.Add(id, node = new()
                        {
                            id = id,
                            name = type.Name,
                            href = $"{id}.md",
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
                        yield return node;
                    }

                    foreach (var child in type.GetTypeMembers())
                    {
                        foreach (var item in CreateToc(child, compilation))
                            yield return item;
                    }

                    node.symbols.Add((symbol, compilation));
                    allSymbols.Add((symbol, compilation));
                    break;

                default:
                    throw new NotSupportedException($"Unknown symbol {symbol}");
            }
        }

        static void SortToc(List<TocNode> items, bool root)
        {
            items.Sort((a, b) => a.type.CompareTo(b.type) is var r && r is 0 ? a.name.CompareTo(b.name) : r);

            if (!root)
            {
                InsertCategory(TocNodeType.Namespace, "Namespaces");
                InsertCategory(TocNodeType.Class, "Classes");
                InsertCategory(TocNodeType.Struct, "Structs");
                InsertCategory(TocNodeType.Interface, "Interfaces");
                InsertCategory(TocNodeType.Enum, "Enums");
                InsertCategory(TocNodeType.Delegate, "Delegates");
            }

            foreach (var item in items)
            {
                if (item.items is not null)
                    SortToc(item.items, root: false);
            }

            void InsertCategory(TocNodeType type, string name)
            {
                if (items.FirstOrDefault(i => i.type == type) is { } node)
                    items.Insert(items.IndexOf(node), new() { name = name });
            }
        }

        static IEnumerable<(string id, List<(ISymbol symbol, Compilation compilation)> symbols)> EnumerateToc(List<TocNode> items)
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

        void SaveTocNode(string id, List<(ISymbol symbol, Compilation compilation)> symbols)
        {
            switch (symbols[0].symbol)
            {
                case INamespaceSymbol ns: SaveNamespace(id, symbols); break;
                case INamedTypeSymbol type: SaveNamedType(type, symbols[0].compilation); break;
                default: throw new NotSupportedException($"Unknown symbol type kind {symbols[0].symbol}");
            }
        }

        void SaveNamespace(string id, List<(ISymbol symbol, Compilation compilation)> symbols)
        {
            var sb = new StringBuilder();
            var symbol = symbols[0].symbol;
            var compilation = symbols[0].compilation;

            var namespaceSymbols = symbols.Select(n => n.symbol).ToHashSet(SymbolEqualityComparer.Default);
            var types = (
                from s in allSymbols
                where s.symbol.Kind is SymbolKind.NamedType && SymbolEqualityComparer.Default.Equals(s.symbol.ContainingNamespace, symbol)
                select (symbol: (INamedTypeSymbol)s.symbol, s.compilation)).ToList();

            sb.AppendLine($"# Namespace {Escape(symbol.ToString()!)}").AppendLine();

            Summary();
            Namespaces();
            Types(t => t.TypeKind is TypeKind.Class, "Classes");
            Types(t => t.TypeKind is TypeKind.Struct, "Structs");
            Types(t => t.TypeKind is TypeKind.Interface, "Interfaces");
            Types(t => t.TypeKind is TypeKind.Enum, "Enums");
            Types(t => t.TypeKind is TypeKind.Delegate, "Delegates");

            File.WriteAllText(Path.Combine(config.OutputFolder, $"{id}.md"), sb.ToString());

            void Summary()
            {
                var comment = Comment(symbol, compilation);
                if (!string.IsNullOrEmpty(comment?.Summary))
                    sb.AppendLine(comment.Summary).AppendLine();
            }

            void Namespaces()
            {
                var items = symbols
                    .SelectMany(n => ((INamespaceSymbol)n.symbol).GetNamespaceMembers().Select(symbol => (symbol, n.compilation)))
                    .DistinctBy(n => n.symbol.Name)
                    .OrderBy(n => n.symbol.Name)
                    .ToList();

                if (items.Count is 0)
                    return;

                sb.AppendLine($"## Namespaces").AppendLine();

                foreach (var (symbol, compilation) in items)
                {
                    sb.AppendLine(Link(symbol, compilation)).AppendLine();
                    var comment = Comment(symbol, compilation);
                    if (!string.IsNullOrEmpty(comment?.Summary))
                        sb.AppendLine(comment.Summary).AppendLine();
                }
            }

            void Types(Func<INamedTypeSymbol, bool> predicate, string headingText)
            {
                var items = types.Where(t => predicate(t.symbol)).ToList();
                if (items.Count == 0)
                    return;

                sb.AppendLine($"## {headingText}").AppendLine();

                foreach (var (symbol, compilation) in items)
                {
                    sb.AppendLine(Link(symbol, compilation)).AppendLine();
                    var comment = Comment(symbol, compilation);
                    if (!string.IsNullOrEmpty(comment?.Summary))
                        sb.AppendLine(comment.Summary).AppendLine();
                }
            }
        }

        void SaveNamedType(INamedTypeSymbol symbol, Compilation compilation)
        {
            var sb = new StringBuilder();
            var comment = Comment(symbol, compilation);

            switch (symbol.TypeKind)
            {
                case TypeKind.Enum: Enum(); break;
                case TypeKind.Delegate: Delegate(); break;
                case TypeKind.Interface or TypeKind.Structure or TypeKind.Class: Class(); break;
                default: throw new NotSupportedException($"Unknown symbol type kind {symbol.TypeKind}");
            }

            var filename = Path.Combine(config.OutputFolder, VisitorHelper.GetId(symbol).Replace('`', '-') + ".md");
            File.WriteAllText(filename, sb.ToString());

            void Enum()
            {
                sb.AppendLine($"# Enum {Escape(symbol.Name)}").AppendLine();

                Info();
                Summary(comment);
                Syntax(symbol);

                EnumFields();

                Examples(comment);
                Remarks(comment);
                SeeAlsos(comment);
            }

            void Delegate()
            {
                sb.AppendLine($"# Delegate {Escape(symbol.Name)}").AppendLine();

                Info();
                Summary(comment);
                Syntax(symbol);

                var invokeMethod = symbol.DelegateInvokeMethod!;
                Parameters(invokeMethod, comment);
                Returns(invokeMethod, comment);
                TypeParameters(invokeMethod.ContainingType, comment);

                Examples(comment);
                Remarks(comment);
                SeeAlsos(comment);
            }

            void Class()
            {
                var typeHeader = symbol.TypeKind switch
                {
                    TypeKind.Interface => "Interface",
                    TypeKind.Class => "Class",
                    TypeKind.Struct => "Struct",
                    _ => throw new InvalidOperationException(),
                };

                sb.AppendLine($"# {typeHeader} {Escape(symbol.Name)}").AppendLine();

                Info();
                Summary(comment);
                Syntax(symbol);

                TypeParameters(symbol, comment);
                Inheritance();
                Derived();
                Implements();
                InheritedMembers();
                ExtensionMethods();

                Examples(comment);
                Remarks(comment);

                Methods(SymbolHelper.IsConstructor, "Constructors");
                Fields();
                Properties();
                Methods(SymbolHelper.IsMethod, "Methods");
                Events();
                Methods(SymbolHelper.IsOperator, "Operators");

                SeeAlsos(comment);
            }

            void Inheritance()
            {
                var items = new List<ISymbol>();
                for (var type = symbol; type != null; type = type.BaseType)
                    items.Add(type);

                if (items.Count <= 1)
                    return;

                items.Reverse();
                sb.AppendLine($"#### Inheritance");
                List(" \u2190 ", items);
            }

            void Derived()
            {
                var items = (
                    from s in allSymbols
                    where s.symbol.Kind is SymbolKind.NamedType && SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)s.symbol).BaseType, symbol)
                    select s.symbol).ToList();

                if (items.Count is 0)
                    return;

                sb.AppendLine($"#### Derived");
                List(", ", items);
            }

            void Implements()
            {
                var items = symbol.AllInterfaces.Where(filter.IncludeApi).ToList();
                if (items.Count is 0)
                    return;

                sb.AppendLine($"#### Implements");
                List(", ", items);
            }

            void InheritedMembers()
            {
                var items = symbol.GetInheritedMembers(filter).ToList();
                if (items.Count is 0)
                    return;

                sb.AppendLine($"#### Inherited Members");
                List(", ", items);
            }

            void ExtensionMethods()
            {
                var items = extensionMethods
                    .Where(m => m.Language == symbol.Language)
                    .Select(m => m.ReduceExtensionMethod(symbol))
                    .OfType<IMethodSymbol>()
                    .OrderBy(i => i.Name)
                    .ToList();

                if (items.Count is 0)
                    return;

                sb.AppendLine($"#### Extension Methods");
                List(", ", items);
            }

            void List(string separator, IEnumerable<ISymbol> items)
            {
                sb.AppendLine(string.Join(separator, items.Select(i => "\n" + Link(i, compilation)))).AppendLine();
            }

            void Parameters(ISymbol symbol, XmlComment? comment, string heading = "##")
            {
                var parameters = symbol.GetParameters();
                if (!parameters.Any())
                    return;

                sb.AppendLine($"{heading} Parameters").AppendLine();

                foreach (var param in parameters)
                {
                    sb.AppendLine($"`{Escape(param.Name)}` {Link(param.Type, compilation)}").AppendLine();

                    if (comment?.Parameters?.TryGetValue(param.Name, out var value) ?? false)
                        sb.AppendLine($"{value}").AppendLine();
                }
            }

            void Returns(IMethodSymbol symbol, XmlComment? comment, string heading = "##")
            {
                if (symbol.ReturnType is null || symbol.ReturnType.SpecialType is SpecialType.System_Void)
                    return;

                sb.AppendLine($"{heading} Returns").AppendLine();
                sb.AppendLine(Link(symbol.ReturnType, compilation)).AppendLine();

                if (!string.IsNullOrEmpty(comment?.Returns))
                    sb.AppendLine($"{comment.Returns}").AppendLine();
            }

            void TypeParameters(ISymbol symbol, XmlComment? comment, string heading = "##")
            {
                if (symbol.GetTypeParameters() is { } typeParameters && typeParameters.Length is 0)
                    return;

                sb.AppendLine($"{heading} Type Parameters").AppendLine();

                foreach (var param in typeParameters)
                {
                    sb.AppendLine($"`{Escape(param.Name)}`").AppendLine();

                    if (comment?.TypeParameters?.TryGetValue(param.Name, out var value) ?? false)
                        sb.AppendLine($"{value}").AppendLine();
                }
            }

            void Methods(Func<IMethodSymbol, bool> predicate, string headingText)
            {
                var items = symbol.GetMembers().OfType<IMethodSymbol>().Where(filter.IncludeApi)
                    .Where(predicate).OrderBy(m => m.Name).ToList();

                if (!items.Any())
                    return;

                sb.AppendLine($"## {headingText}").AppendLine();

                foreach (var item in items)
                    Method(item);

                void Method(IMethodSymbol symbol)
                {
                    sb.AppendLine($"### {Escape(SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp))}").AppendLine();

                    var comment = Comment(symbol, compilation);
                    Summary(comment);
                    Syntax(symbol);

                    Parameters(symbol, comment, "####");
                    Returns(symbol, comment, "####");
                    TypeParameters(symbol, comment, "####");

                    Examples(comment, "####");
                    Remarks(comment, "####");
                    Exceptions(comment, "####");
                    SeeAlsos(comment, "####");
                }
            }

            void Fields()
            {
                var items = symbol.GetMembers().OfType<IFieldSymbol>().Where(filter.IncludeApi).OrderBy(m => m.Name).ToList();
                if (!items.Any())
                    return;

                sb.AppendLine($"## Fields").AppendLine();

                foreach (var item in items)
                    Field(item);

                void Field(IFieldSymbol symbol)
                {
                    sb.AppendLine($"### {Escape(SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp))}").AppendLine();

                    var comment = Comment(symbol, compilation);
                    Summary(comment);
                    Syntax(symbol);

                    sb.AppendLine("Field Value").AppendLine();
                    sb.AppendLine(Link(symbol.Type, compilation)).AppendLine();

                    Examples(comment, "####");
                    Remarks(comment, "####");
                    Exceptions(comment, "####");
                    SeeAlsos(comment, "####");
                }
            }

            void Properties()
            {
                var items = symbol.GetMembers().OfType<IPropertySymbol>().Where(filter.IncludeApi).OrderBy(m => m.Name).ToList();
                if (!items.Any())
                    return;

                sb.AppendLine($"## Properties").AppendLine();

                foreach (var item in items)
                    Property(item);

                void Property(IPropertySymbol symbol)
                {
                    sb.AppendLine($"### {Escape(SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp))}").AppendLine();

                    var comment = Comment(symbol, compilation);
                    Summary(comment);
                    Syntax(symbol);

                    sb.AppendLine("Property Value").AppendLine();
                    sb.AppendLine(Link(symbol.Type, compilation)).AppendLine();

                    Examples(comment, "####");
                    Remarks(comment, "####");
                    Exceptions(comment, "####");
                    SeeAlsos(comment, "####");
                }
            }

            void Events()
            {
                var items = symbol.GetMembers().OfType<IEventSymbol>().Where(filter.IncludeApi).OrderBy(m => m.Name).ToList();
                if (!items.Any())
                    return;

                sb.AppendLine($"## Events").AppendLine();

                foreach (var item in items)
                    Event(item);

                void Event(IEventSymbol symbol)
                {
                    sb.AppendLine($"### {Escape(SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp))}").AppendLine();

                    var comment = Comment(symbol, compilation);
                    Summary(comment);
                    Syntax(symbol);

                    sb.AppendLine("Event Type").AppendLine();
                    sb.AppendLine(Link(symbol.Type, compilation)).AppendLine();

                    Examples(comment, "####");
                    Remarks(comment, "####");
                    Exceptions(comment, "####");
                    SeeAlsos(comment, "####");
                }
            }

            void EnumFields()
            {
                var items = symbol.GetMembers().OfType<IFieldSymbol>().Where(filter.IncludeApi).ToList();
                if (!items.Any())
                    return;

                if (config.EnumSortOrder is EnumSortOrder.Alphabetic)
                    items = items.OrderBy(m => m.Name).ToList();

                sb.AppendLine($"## Fields").AppendLine();

                foreach (var item in items)
                {
                    sb.AppendLine($"### `{Escape(item.Name)} = {item.ConstantValue}`").AppendLine();

                    if (Comment(item, compilation) is { } comment)
                        sb.AppendLine($"{Escape(comment.Summary)}").AppendLine();
                }
            }

            void Info()
            {
                sb.AppendLine($"__Namespace:__ {Link(symbol.ContainingNamespace, compilation)}  ");
                sb.AppendLine($"__Assembly:__ {symbol.ContainingAssembly.Name}.dll").AppendLine();
            }

            void Summary(XmlComment? comment)
            {
                if (!string.IsNullOrEmpty(comment?.Summary))
                    sb.AppendLine(comment.Summary).AppendLine();
            }

            void Syntax(ISymbol symbol)
            {
                var syntax = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, filter);
                sb.AppendLine("```csharp").AppendLine(syntax).AppendLine("```").AppendLine();
            }

            void Examples(XmlComment? comment, string heading = "##")
            {
                if (comment?.Examples?.Count > 0)
                {
                    sb.AppendLine($"{heading} Examples").AppendLine();

                    foreach (var example in comment.Examples)
                        sb.AppendLine(example).AppendLine();
                }
            }

            void Remarks(XmlComment? comment, string heading = "##")
            {
                if (!string.IsNullOrEmpty(comment?.Remarks))
                {
                    sb.AppendLine($"{heading} Remarks").AppendLine();
                    sb.AppendLine(comment.Remarks).AppendLine();
                }
            }

            void Exceptions(XmlComment? comment, string heading = "##")
            {
                if (comment?.Exceptions?.Count > 0)
                {
                    sb.AppendLine($"{heading} Exceptions").AppendLine();

                    foreach (var exception in comment.Exceptions)
                    {
                        sb.AppendLine(Cref(exception.CommentId)).AppendLine();
                        sb.AppendLine(exception.Description).AppendLine();
                    }
                }
            }

            void SeeAlsos(XmlComment? comment, string heading = "##")
            {
                if (comment?.SeeAlsos?.Count > 0)
                {
                    sb.AppendLine($"{heading} See Also").AppendLine();

                    foreach (var seealso in comment.SeeAlsos)
                    {
                        var content = seealso.LinkType switch
                        {
                            LinkType.CRef => Cref(seealso.CommentId),
                            LinkType.HRef => $"<{Escape(seealso.LinkId)}>",
                            _ => throw new NotSupportedException($"{seealso.LinkType}"),
                        };
                        sb.AppendLine(content).AppendLine();
                    }
                }
            }

            string Cref(string commentId)
            {
                return DocumentationCommentId.GetFirstSymbolForDeclarationId(commentId, compilation) is { } symbol ? Link(symbol, compilation) : "";
            }
        }

        string Link(ISymbol symbol, Compilation compilation)
        {
            return symbol.Kind is SymbolKind.Method or SymbolKind.Namespace or SymbolKind.Event or SymbolKind.Property or SymbolKind.Field ? ShortLink() : FullLink();

            string ShortLink()
            {
                var title = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
                var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, SymbolUrlKind.Markdown, allAssemblies);
                return string.IsNullOrEmpty(url) ? Escape(title) : $"[{Escape(title)}]({Escape(url)})";
            }

            string FullLink()
            {
                var parts = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp);
                var linkItems = SymbolFormatter.ToLinkItems(parts, compilation, config.MemberLayout, allAssemblies, overload: false, SymbolUrlKind.Markdown);

                return string.Concat(linkItems.Select(i =>
                    string.IsNullOrEmpty(i.Href) ? Escape(i.DisplayName) : $"[{Escape(i.DisplayName)}]({Escape(i.Href)})"));
            }
        }

        XmlComment? Comment(ISymbol symbol, Compilation compilation)
        {
            var src = VisitorHelper.GetSourceDetail(symbol, compilation);
            var context = new XmlCommentParserContext
            {
                SkipMarkup = config.ShouldSkipMarkup,
                AddReferenceDelegate = (a, b) => { },
                Source = src,
                ResolveCode = ResolveCode,
            };

            var comment = symbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true);
            return XmlComment.Parse(comment.FullXmlFragment, context);

            string? ResolveCode(string source)
            {
                var basePath = config.CodeSourceBasePath ?? (
                    src?.Path is { } sourcePath
                        ? Path.GetDirectoryName(Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, sourcePath)))
                        : null);

                var path = Path.GetFullPath(Path.Combine(basePath ?? "", source));
                if (!File.Exists(path))
                {
                    Logger.LogWarning($"Source file '{path}' not found.");
                    return null;
                }

                return File.ReadAllText(path);
            }
        }

        static string Escape(string text)
        {
            return text;
        }
    }
}
