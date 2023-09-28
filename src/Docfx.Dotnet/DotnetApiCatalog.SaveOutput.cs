// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
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
        internal List<(ISymbol symbol, Compilation compilation)> symbols = new();
    }

    public static void Save(Func<string, string, OutputWriter> output, List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Logger.LogWarning($"Markdown output format is experimental.");

        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.symbol.FindExtensionMethods(filter)).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.symbol), SymbolEqualityComparer.Default);
        var commentCache = new ConcurrentDictionary<ISymbol, XmlComment>(SymbolEqualityComparer.Default);

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

            switch (symbol)
            {
                case INamespaceSymbol ns when ns.IsGlobalNamespace:
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
                }

                foreach (var child in type.GetTypeMembers())
                {
                    foreach (var item in CreateToc(child, compilation))
                        yield return item;
                }

                if (config.MemberLayout is MemberLayout.SeparatePages && type.TypeKind is TypeKind.Class or TypeKind.Interface or TypeKind.Struct)
                {
                    node.items ??= new();
                    foreach (var member in type.GetMembers())
                        node.items.AddRange(CreateToc(member, compilation));
                }

                node.symbols.Add((symbol, compilation));
                allSymbols.Add((symbol, compilation));

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
                        href = $"{id}.md",
                        containsLeafNodes = true,
                        type = type,
                    });
                    yield return node;
                }

                node.symbols.Add((symbol, compilation));
            }
        }

        static void SortToc(List<TocNode> items, bool root)
        {
            items.Sort((a, b) => a.type.CompareTo(b.type) is var r && r is 0 ? a.name.CompareTo(b.name) : r);

            if (!root)
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
            var writer = output(config.OutputFolder, id);
            var symbol = symbols[0].symbol;
            var compilation = symbols[0].compilation;
            var comment = Comment(symbol, compilation);

            switch (symbols[0].symbol)
            {
                case INamespaceSymbol ns:
                    Namespace();
                    break;

                case INamedTypeSymbol type:
                    switch (type.TypeKind)
                    {
                        case TypeKind.Enum: Enum(type); break;
                        case TypeKind.Delegate: Delegate(type); break;
                        case TypeKind.Interface or TypeKind.Structure or TypeKind.Class: ClassLike(type); break;
                        default: throw new NotSupportedException($"Unknown symbol type kind {type.TypeKind}");
                    }
                    break;

                case IFieldSymbol:
                    MemberHeader("Field");
                    foreach (var (s, c) in symbols)
                        Field((IFieldSymbol)s, c, 2);
                    break;

                case IPropertySymbol:
                    MemberHeader("Property");
                    foreach (var (s, c) in symbols)
                        Property((IPropertySymbol)s, c, 2);
                    break;

                case IEventSymbol:
                    MemberHeader("Event");
                    foreach (var (s, c) in symbols)
                        Event((IEventSymbol)s, c, 2);
                    break;

                case IMethodSymbol method:
                    MemberHeader(method switch
                    {
                        _ when SymbolHelper.IsConstructor(method) => "Constructor",
                        _ when SymbolHelper.IsOperator(method) => "Operator",
                        _ when SymbolHelper.IsMember(method) => "Method",
                        _ => throw new NotSupportedException($"Unknown method type {method.MethodKind}"),
                    }); ;
                    foreach (var (s, c) in symbols)
                        Method((IMethodSymbol)s, c, 2);
                    break;

                default:
                    throw new NotSupportedException($"Unknown symbol type kind {symbols[0].symbol}");
            }

            writer.End();

            void Namespace()
            {
                var namespaceSymbols = symbols.Select(n => n.symbol).ToHashSet(SymbolEqualityComparer.Default);
                var types = (
                    from s in allSymbols
                    where s.symbol.Kind is SymbolKind.NamedType && namespaceSymbols.Contains(s.symbol.ContainingNamespace)
                    select (symbol: (INamedTypeSymbol)s.symbol, s.compilation)).ToList();

                writer.Heading(1, $"Namespace {symbol}");

                Summary(comment);
                Namespaces();
                Types(t => t.TypeKind is TypeKind.Class, "Classes");
                Types(t => t.TypeKind is TypeKind.Struct, "Structs");
                Types(t => t.TypeKind is TypeKind.Interface, "Interfaces");
                Types(t => t.TypeKind is TypeKind.Enum, "Enums");
                Types(t => t.TypeKind is TypeKind.Delegate, "Delegates");

                void Namespaces()
                {
                    var items = symbols
                        .SelectMany(n => ((INamespaceSymbol)n.symbol).GetNamespaceMembers().Select(symbol => (symbol, n.compilation)))
                        .DistinctBy(n => n.symbol.Name)
                        .OrderBy(n => n.symbol.Name)
                        .ToList();

                    if (items.Count is 0)
                        return;

                    writer.Heading(3, "Namespaces");

                    foreach (var (symbol, compilation) in items)
                    {
                        writer.Text(ShortLink(symbol, compilation));
                        var comment = Comment(symbol, compilation);
                        if (!string.IsNullOrEmpty(comment?.Summary))
                            writer.Markdown(comment.Summary);
                    }
                }

                void Types(Func<INamedTypeSymbol, bool> predicate, string headingText)
                {
                    var items = types.Where(t => predicate(t.symbol)).ToList();
                    if (items.Count == 0)
                        return;

                    writer.Heading(3,  headingText);
                    foreach (var (symbol, compilation) in items)
                    {
                        writer.Text(ShortLink(symbol, compilation));
                        var comment = Comment(symbol, compilation);
                        if (!string.IsNullOrEmpty(comment?.Summary))
                            writer.Markdown(comment.Summary);
                    }
                }
            }

            void Enum(INamedTypeSymbol type)
            {
                writer.Heading(3, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp));

                writer.Facts(Facts().ToArray());
                Summary(comment);
                Syntax(symbol);

                ExtensionMethods(type);

                EnumFields(type);

                Examples(comment);
                Remarks(comment);
                SeeAlsos(comment);
            }

            void Delegate(INamedTypeSymbol type)
            {
                writer.Heading(1, $"Delegate {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp)}");

                writer.Facts(Facts().ToArray());
                Summary(comment);
                Syntax(symbol);

                var invokeMethod = type.DelegateInvokeMethod!;
                Parameters(invokeMethod, comment);
                Returns(invokeMethod, comment);
                TypeParameters(invokeMethod.ContainingType, comment);

                ExtensionMethods(type);

                Examples(comment);
                Remarks(comment);
                SeeAlsos(comment);
            }

            void ClassLike(INamedTypeSymbol type)
            {
                var typeHeader = type.TypeKind switch
                {
                    TypeKind.Interface => "Interface",
                    TypeKind.Class => "Class",
                    TypeKind.Struct => "Struct",
                    _ => throw new InvalidOperationException(),
                };

                writer.Heading(1, $"{typeHeader} {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp)}");

                writer.Facts(Facts().ToArray());
                Summary(comment);
                Syntax(symbol);

                TypeParameters(symbol, comment);
                Inheritance();
                Derived();
                Implements();
                InheritedMembers();
                ExtensionMethods(type);

                Examples(comment);
                Remarks(comment);

                Methods(SymbolHelper.IsConstructor, "Constructors");
                Fields();
                Properties();
                Methods(SymbolHelper.IsMethod, "Methods");
                Events();
                Methods(SymbolHelper.IsOperator, "Operators");

                SeeAlsos(comment);

                void Fields()
                {
                    var items = (
                        from s in symbols
                        from symbol in ((INamedTypeSymbol)s.symbol).GetMembers().OfType<IFieldSymbol>()
                        where filter.IncludeApi(symbol)
                        orderby symbol.Name
                        select (symbol, s.compilation)).ToList();

                    if (items.Count is 0)
                        return;

                    writer.Heading(2, "Fields");

                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        MemberSummaryList(items);
                        return;
                    }

                    foreach (var (s, c) in items)
                        Field(s, c, 3);
                }

                void Properties()
                {
                    var items = (
                        from s in symbols
                        from symbol in ((INamedTypeSymbol)s.symbol).GetMembers().OfType<IPropertySymbol>()
                        where filter.IncludeApi(symbol)
                        orderby symbol.Name
                        select (symbol, s.compilation)).ToList();

                    if (items.Count is 0)
                        return;

                    writer.Heading(2, "Properties");

                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        MemberSummaryList(items);
                        return;
                    }

                    foreach (var (s, c) in items)
                        Property(s, c, 3);
                }

                void Methods(Func<IMethodSymbol, bool> predicate, string headingText)
                {
                    var items = (
                        from s in symbols
                        from symbol in ((INamedTypeSymbol)s.symbol).GetMembers().OfType<IMethodSymbol>()
                        where filter.IncludeApi(symbol) && predicate(symbol)
                        orderby symbol.Name
                        select (symbol, s.compilation)).ToList();

                    if (items.Count is 0)
                        return;

                    writer.Heading(2, "headingText");
                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        MemberSummaryList(items);
                        return;
                    }

                    foreach (var (s, c) in items)
                        Method(s, c, 3);
                }

                void Events()
                {
                    var items = (
                        from s in symbols
                        from symbol in ((INamedTypeSymbol)s.symbol).GetMembers().OfType<IEventSymbol>()
                        where filter.IncludeApi(symbol)
                        orderby symbol.Name
                        select (symbol, s.compilation)).ToList();

                    if (items.Count is 0)
                        return;

                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        MemberSummaryList(items);
                        return;
                    }

                    foreach (var (s, c) in items)
                        Event(s, c, 3);
                }

                void MemberSummaryList<T>(IEnumerable<(T, Compilation)> symbols) where T : ISymbol
                {
                    foreach (var (s, c) in symbols)
                    {
                        writer.Text(NameOnlyLink(s, c));
                        Summary(Comment(s, c));
                    }
                }

                void Inheritance()
                {
                    var items = new List<ISymbol>();
                    for (var i = type; i is not null && i.SpecialType is not SpecialType.System_ValueType; i = i.BaseType)
                        items.Add(i);

                    if (items.Count <= 1)
                        return;

                    items.Reverse();
                    writer.Heading(6, "Inheritance");
                    List(items, ListDelimiter.LeftArrow);
                }

                void Derived()
                {
                    var items = (
                        from s in allSymbols
                        where s.symbol.Kind is SymbolKind.NamedType && SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)s.symbol).BaseType, symbol)
                        select s.symbol).ToList();

                    if (items.Count is 0)
                        return;

                    writer.Heading(6, "Derived");
                    List(items);
                }

                void Implements()
                {
                    var items = type.AllInterfaces.Where(filter.IncludeApi).ToList();
                    if (items.Count is 0)
                        return;

                    writer.Heading(6, "Implements");
                    List(items);
                }

                void InheritedMembers()
                {
                    var items = type.GetInheritedMembers(filter).ToList();
                    if (items.Count is 0)
                        return;

                    writer.Heading(6, "Inherited Members");
                    List(items);
                }
            }

            void MemberHeader(string headingText)
            {
                writer.Heading(1, $"{headingText} {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp, overload: true)}");
                writer.Facts(Facts().ToArray());
            }

            void ExtensionMethods(INamedTypeSymbol type)
            {
                var items = extensionMethods
                    .Where(m => m.Language == symbol.Language)
                    .Select(m => m.ReduceExtensionMethod(type))
                    .OfType<IMethodSymbol>()
                    .OrderBy(i => i.Name)
                    .ToList();

                if (items.Count is 0)
                    return;

                writer.Heading(6, "Extension Methods");
                List(items);
            }

            void List(IEnumerable<ISymbol> items, ListDelimiter delimiter = default)
            {
                writer.JumpList(delimiter, items.Select(i => ShortLink(i, compilation)).ToArray());
            }

            void Parameters(ISymbol symbol, XmlComment? comment, int headingLevel = 2)
            {
                var parameters = symbol.GetParameters();
                if (!parameters.Any())
                    return;

                writer.Heading(headingLevel, "Parameters");
                writer.ParameterList(parameters.Select(ToParameter).ToArray());

                Parameter ToParameter(IParameterSymbol param)
                {
                    var docs = comment?.Parameters is { } p && p.TryGetValue(param.Name, out var value) ? value : null;
                    return new(param.Name, FullLink(param.Type, compilation), null, docs);
                }
            }

            void Returns(IMethodSymbol symbol, XmlComment? comment, int headingLevel = 2)
            {
                if (symbol.ReturnType is null || symbol.ReturnType.SpecialType is SpecialType.System_Void)
                    return;

                writer.Heading(headingLevel, "Returns");
                writer.Text(FullLink(symbol.ReturnType, compilation));

                if (!string.IsNullOrEmpty(comment?.Returns))
                    writer.Markdown($"{comment.Returns}");
            }

            void TypeParameters(ISymbol symbol, XmlComment? comment, int headingLevel = 2)
            {
                if (symbol.GetTypeParameters() is { } typeParameters && typeParameters.Length is 0)
                    return;

                writer.Heading(headingLevel, "Type Parameters");
                writer.ParameterList(typeParameters.Select(ToParameter).ToArray());

                Parameter ToParameter(ITypeParameterSymbol param)
                {
                    var docs = comment?.TypeParameters is { } p && p.TryGetValue(param.Name, out var value) ? value : null;
                    return new(param.Name, docs: docs);
                }
            }

            void Method(IMethodSymbol symbol, Compilation compilation, int headingLevel)
            {
                var fragment = Regex.Replace(VisitorHelper.GetId(symbol), @"\W", "_");
                writer.Heading(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), fragment);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                Parameters(symbol, comment, headingLevel + 1);
                Returns(symbol, comment, headingLevel + 1);
                TypeParameters(symbol, comment, headingLevel + 1);

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void Field(IFieldSymbol symbol, Compilation compilation, int headingLevel)
            {
                var fragment = Regex.Replace(VisitorHelper.GetId(symbol), @"\W", "_");
                writer.Heading(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), fragment);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                writer.Heading(headingLevel + 1, "Field Value");
                writer.Text(FullLink(symbol.Type, compilation));

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void Property(IPropertySymbol symbol, Compilation compilation, int headingLevel)
            {
                var fragment = Regex.Replace(VisitorHelper.GetId(symbol), @"\W", "_");
                writer.Heading(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), fragment);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                writer.Heading(headingLevel + 1, "Property Value");
                writer.Text(FullLink(symbol.Type, compilation));

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void Event(IEventSymbol symbol, Compilation compilation, int headingLevel)
            {
                var fragment = Regex.Replace(VisitorHelper.GetId(symbol), @"\W", "_");
                writer.Heading(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), fragment);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                writer.Heading(headingLevel + 1, "Event Type");
                writer.Text(FullLink(symbol.Type, compilation));

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void EnumFields(INamedTypeSymbol type)
            {
                var items = type.GetMembers().OfType<IFieldSymbol>().Where(filter.IncludeApi).ToList();
                if (!items.Any())
                    return;

                if (config.EnumSortOrder is EnumSortOrder.Alphabetic)
                    items = items.OrderBy(m => m.Name).ToList();

                writer.Heading(2, "Fields");
                writer.ParameterList(items.Select(ToParameter).ToArray());

                Parameter ToParameter(IFieldSymbol item)
                {
                    var docs = Comment(item, compilation) is { } comment ? comment.Summary : null;
                    return new(item.Name, null, $"{item.ConstantValue}", docs);
                }
            }

            IEnumerable<Fact> Facts()
            {
                yield return new("Namespace", ShortLink(symbol.ContainingNamespace, compilation));

                var assemblies = symbols.Select(s => s.symbol.ContainingAssembly.Name).Where(n => n != "?").Distinct().Select(n => $"{n}.dll").ToList();
                if (assemblies.Count > 0)
                    yield return new("Assembly", new[] { new TextSpan(string.Join(", ", assemblies)) });
            }

            void Summary(XmlComment? comment)
            {
                if (!string.IsNullOrEmpty(comment?.Summary))
                    writer.Markdown(comment.Summary);
            }

            void Syntax(ISymbol symbol)
            {
                var syntax = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, filter);
                writer.Declaration(syntax, "csharp");
            }

            void Examples(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.Examples?.Count > 0)
                {
                    writer.Heading(headingLevel, "Examples");

                    foreach (var example in comment.Examples)
                        writer.Markdown(example);
                }
            }

            void Remarks(XmlComment? comment, int headingLevel = 2)
            {
                if (!string.IsNullOrEmpty(comment?.Remarks))
                {
                    writer.Heading(headingLevel, "Remarks");
                    writer.Markdown(comment.Remarks);
                }
            }

            void Exceptions(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.Exceptions?.Count > 0)
                {
                    writer.Heading(headingLevel, "Exceptions");

                    foreach (var exception in comment.Exceptions)
                    {
                        writer.Text(Cref(exception.CommentId));
                        writer.Markdown(exception.Description);
                    }
                }
            }

            void SeeAlsos(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.SeeAlsos?.Count > 0)
                {
                    writer.Heading(headingLevel, "See Also");

                    foreach (var seealso in comment.SeeAlsos)
                    {
                        writer.Text(seealso.LinkType switch
                        {
                            LinkType.CRef => Cref(seealso.CommentId),
                            LinkType.HRef => new[] { new TextSpan(seealso.LinkId, (string?)seealso.LinkId) },
                            _ => throw new NotSupportedException($"{seealso.LinkType}"),
                        });
                    }
                }
            }

            TextSpan[] Cref(string commentId)
            {
                return DocumentationCommentId.GetFirstSymbolForDeclarationId(commentId, compilation) is { } symbol ? FullLink(symbol, compilation) : Array.Empty<TextSpan>();
            }
        }

        TextSpan[] ShortLink(ISymbol symbol, Compilation compilation)
        {
            var title = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
            var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, SymbolUrlKind.Markdown, allAssemblies);
            return new[] { new TextSpan(title, url) };
        }

        TextSpan[] FullLink(ISymbol symbol, Compilation compilation)
        {
            var parts = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp);
            var linkItems = SymbolFormatter.ToLinkItems(parts, compilation, config.MemberLayout, allAssemblies, overload: false, SymbolUrlKind.Markdown);

            return linkItems.Select(i => new TextSpan(i.DisplayName, (string?)i.Href)).ToArray();
        }

        TextSpan[] NameOnlyLink(ISymbol symbol, Compilation compilation)
        {
            var title = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp);
            var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, SymbolUrlKind.Markdown, allAssemblies);
            return new[] { new TextSpan(title, url) };
        }

        XmlComment? Comment(ISymbol symbol, Compilation compilation)
        {
            // Cache XML comment to avoid duplicated parsing and warnings
            return commentCache.GetOrAdd(symbol, symbol =>
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
            });
        }
    }
}
