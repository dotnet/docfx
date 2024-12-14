// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Docfx.Build.ApiPage;
using Docfx.Common;
using Docfx.Common.Git;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using HtmlAgilityPack;
using Microsoft.CodeAnalysis;
using OneOf;

#nullable enable

namespace Docfx.Dotnet;

partial class DotnetApiCatalog
{
    private static void CreatePages(Action<string, string, ApiPage> output, List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.symbol.FindExtensionMethods(filter)).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.symbol), SymbolEqualityComparer.Default);
        var commentCache = new ConcurrentDictionary<ISymbol, XmlComment>(SymbolEqualityComparer.Default);
        var symbolUrlKind = config.OutputFormat is MetadataOutputFormat.Markdown ? SymbolUrlKind.Markdown : SymbolUrlKind.Html;
        var toc = CreateToc(assemblies, config, options);
        var allSymbols = EnumerateToc(toc).SelectMany(node => node.symbols).ToList();
        var allNamespaceSymbols = allSymbols.Where(i => i.symbol.Kind is SymbolKind.Namespace).ToHashSet();
        allSymbols.Sort((a, b) => a.symbol.Name.CompareTo(b.symbol.Name));

        Parallel.ForEach(EnumerateToc(toc), n => SaveTocNode(n.id, n.symbols));

        Logger.LogInfo($"Export succeed: {EnumerateToc(toc).Count()} items");

        void SaveTocNode(string id, List<(ISymbol symbol, Compilation compilation)> symbols)
        {
            var title = "";
            var body = new List<Block>();
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
                    });
                    foreach (var (s, c) in symbols)
                        Method((IMethodSymbol)s, c, 2);
                    break;

                default:
                    throw new NotSupportedException($"Unknown symbol type kind {symbols[0].symbol}");
            }

            var metadata = new Dictionary<string, OneOf<string, string[]>>();
            if (!string.IsNullOrEmpty(comment?.Summary))
                metadata["description"] = HtmlInnerText(comment.Summary);

            output(config.OutputFolder, id, new ApiPage
            {
                title = title,
                metadata = metadata.Count > 0 ? metadata : null,
                languageId = "csharp",
                body = body.ToArray(),
            });

            void Heading(int level, string title, string? id = null)
            {
                body.Add(level switch
                {
                    1 => (Heading)new H1 { h1 = title, id = id },
                    2 => (Heading)new H2 { h2 = title, id = id },
                    3 => (Heading)new H3 { h3 = title, id = id },
                    4 => (Heading)new H4 { h4 = title, id = id },
                    5 => (Heading)new H5 { h5 = title, id = id },
                    6 => (Heading)new H6 { h6 = title, id = id },
                });
            }

            void Api(int level, string title, ISymbol symbol, Compilation compilation)
            {
                var uid = VisitorHelper.GetId(symbol);
                var id = Regex.Replace(uid, @"\W", "_");
                var commentId = VisitorHelper.GetCommentId(symbol);
                var source = config.DisableGitFeatures ? null : VisitorHelper.GetSourceDetail(symbol, compilation);
                var git = source?.Remote is null ? null
                    : new GitSource(source.Remote.Repo, source.Remote.Branch, source.Remote.Path, source.StartLine + 1);
                var src = git is null ? null : options.SourceUrl?.Invoke(git) ?? GitUtility.GetSourceUrl(git);
                var deprecated = Deprecated(symbol);
                var preview = Preview(symbol);

                body.Add(level switch
                {
                    1 => (Api)new Api1 { api1 = title, id = id, src = src, deprecated = deprecated, preview = preview, metadata = new() { ["uid"] = uid, ["commentId"] = commentId } },
                    2 => (Api)new Api2 { api2 = title, id = id, src = src, deprecated = deprecated, preview = preview, metadata = new() { ["uid"] = uid, ["commentId"] = commentId } },
                    3 => (Api)new Api3 { api3 = title, id = id, src = src, deprecated = deprecated, preview = preview, metadata = new() { ["uid"] = uid, ["commentId"] = commentId } },
                    4 => (Api)new Api4 { api4 = title, id = id, src = src, deprecated = deprecated, preview = preview, metadata = new() { ["uid"] = uid, ["commentId"] = commentId } },
                });
            }

            OneOf<bool, string>? Deprecated(ISymbol symbol)
            {
                if (symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ObsoleteAttribute") is { } obsoleteAttribute)
                    return obsoleteAttribute.ConstructorArguments.FirstOrDefault().Value is string reason && !string.IsNullOrEmpty(reason) ? (OneOf<bool, string>?)reason : true;
                return null;
            }

            OneOf<bool, string>? Preview(ISymbol symbol, ISymbol? originalSymbol = null)
            {
                if (symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ExperimentalAttribute") is { } experimentalAttribute)
                {
                    var diagnosticId = ApiPageMarkdownTemplate.Escape(experimentalAttribute.ConstructorArguments.FirstOrDefault().Value as string ?? "");
                    var urlFormat = experimentalAttribute.NamedArguments.FirstOrDefault(a => a.Key == "UrlFormat").Value.Value as string;
                    var link = string.IsNullOrEmpty(urlFormat) ? diagnosticId : $"[{diagnosticId}]({ApiPageMarkdownTemplate.Escape(string.Format(urlFormat, diagnosticId))})";
                    var message = $"'{originalSymbol ?? symbol}' is for evaluation purposes only and is subject to change or removal in future updates.";
                    return string.IsNullOrEmpty(diagnosticId) ? message : $"{link}: {message}";
                }

                // Search containing namespace, module, assembly for named types
                if (symbol.ContainingSymbol is not null && symbol.Kind is SymbolKind.NamedType or SymbolKind.Namespace or SymbolKind.NetModule or SymbolKind.Assembly)
                    return Preview(symbol.ContainingSymbol, originalSymbol ?? symbol);

                return null;
            }

            void Namespace()
            {
                var namespaceSymbols = symbols.Select(n => n.symbol).ToHashSet(SymbolEqualityComparer.Default);
                var types = (
                    from s in allSymbols
                    where s.symbol.Kind is SymbolKind.NamedType && namespaceSymbols.Contains(s.symbol.ContainingNamespace)
                    select (symbol: (INamedTypeSymbol)s.symbol, s.compilation)).ToList();

                Api(1, title = $"Namespace {symbol}", symbol, compilation);

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
                        .Where(n => allNamespaceSymbols.Contains(n))
                        .DistinctBy(n => n.symbol.Name)
                        .OrderBy(n => n.symbol.Name)
                        .ToList();

                    if (items.Count is 0)
                        return;

                    Heading(3, "Namespaces");
                    SummaryList(items);
                }

                void Types(Func<INamedTypeSymbol, bool> predicate, string headingText)
                {
                    var items = types.Where(t => predicate(t.symbol)).ToList();
                    if (items.Count == 0)
                        return;

                    Heading(3, headingText);
                    SummaryList(items);
                }
            }

            void Enum(INamedTypeSymbol type)
            {
                Api(1, title = $"Enum {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp)}", symbol, compilation);

                body.Add(new Facts { facts = Facts().ToArray() });
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
                Api(1, title = $"Delegate {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp)}", symbol, compilation);

                body.Add(new Facts { facts = Facts().ToArray() });
                Summary(comment);
                Syntax(symbol);

                var invokeMethod = type.DelegateInvokeMethod!;
                Parameters(invokeMethod, comment, 4);
                Returns(invokeMethod, comment, 4);
                TypeParameters(invokeMethod.ContainingType, comment, 4);

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

                Api(1, title = $"{typeHeader} {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp)}", symbol, compilation);

                body.Add(new Facts { facts = Facts().ToArray() });
                Summary(comment);
                Syntax(symbol);

                TypeParameters(symbol, comment, 4);
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

                    Heading(2, "Fields");

                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        SummaryList(items);
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

                    Heading(2, "Properties");

                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        SummaryList(items);
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

                    Heading(2, headingText);
                    if (config.MemberLayout is MemberLayout.SeparatePages)
                    {
                        SummaryList(items);
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
                        SummaryList(items);
                        return;
                    }

                    foreach (var (s, c) in items)
                        Event(s, c, 3);
                }

                void Inheritance()
                {
                    var items = new List<ISymbol>();
                    for (var i = type; i is not null && i.SpecialType is not SpecialType.System_ValueType; i = i.BaseType)
                        items.Add(i);

                    if (items.Count <= 1)
                        return;

                    items.Reverse();

                    Heading(4, "Inheritance");
                    body.Add(new Inheritance { inheritance = items.Select(i => ShortLink(i, compilation)).ToArray() });
                }

                void Derived()
                {
                    var items = (
                        from s in allSymbols
                        where s.symbol.Kind is SymbolKind.NamedType && SymbolEqualityComparer.Default.Equals(((INamedTypeSymbol)s.symbol).BaseType, symbol)
                        select s.symbol).ToList();

                    if (items.Count is 0)
                        return;

                    Heading(4, "Derived");
                    body.Add(new List { list = items.Select(i => ShortLink(i, compilation)).ToArray() });
                }

                void Implements()
                {
                    var items = type.AllInterfaces.Where(filter.IncludeApi).ToList();
                    if (items.Count is 0)
                        return;

                    Heading(4, "Implements");
                    body.Add(new List { list = items.Select(i => ShortLink(i, compilation)).ToArray() });
                }

                void InheritedMembers()
                {
                    var items = type.GetInheritedMembers(filter).ToList();
                    if (items.Count is 0)
                        return;

                    Heading(4, "Inherited Members");
                    body.Add(new List { list = items.Select(i => ShortLink(i, compilation)).ToArray() });
                }
            }

            void MemberHeader(string headingText)
            {
                Api(1, title = $"{headingText} {SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp, overload: true)}", symbol, compilation);
                body.Add(new Facts { facts = Facts().ToArray() });
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

                Heading(4, "Extension Methods");
                body.Add(new List { list = items.Select(i => ShortLink(i, compilation)).ToArray() });
            }

            void SummaryList<T>(IEnumerable<(T, Compilation)> items) where T : ISymbol
            {
                body.Add(new Parameters
                {
                    parameters = items.Select(i =>
                    {
                        var (symbol, compilation) = i;
                        var comment = Comment(symbol, compilation);
                        var type = symbol is INamedTypeSymbol ? ShortLink(symbol, compilation) : NameOnlyLink(symbol, compilation);
                        return new Parameter { type = type, description = comment?.Summary };
                    }).ToArray()
                });
            }

            void Parameters(ISymbol symbol, XmlComment? comment, int headingLevel)
            {
                var parameters = symbol.GetParameters();
                if (!parameters.Any())
                    return;

                Heading(headingLevel, "Parameters");
                body.Add(new Parameters { parameters = parameters.Select(ToParameter).ToArray() });

                Parameter ToParameter(IParameterSymbol param)
                {
                    var docs = comment?.Parameters is { } p && p.TryGetValue(param.Name, out var value) ? value : null;
                    return new() { name = param.Name, type = FullLink(param.Type, compilation), description = docs, optional = param.IsOptional ? true : null };
                }
            }

            void Returns(IMethodSymbol symbol, XmlComment? comment, int headingLevel)
            {
                if (symbol.ReturnType is null || symbol.ReturnType.SpecialType is SpecialType.System_Void)
                    return;

                Heading(headingLevel, "Returns");
                body.Add(new Parameters
                {
                    parameters =
                    [
                        new Parameter() { type = FullLink(symbol.ReturnType, compilation), description = comment?.Returns }
                    ]
                });
            }

            void TypeParameters(ISymbol symbol, XmlComment? comment, int headingLevel)
            {
                if (symbol.GetTypeParameters() is { } typeParameters && typeParameters.Length is 0)
                    return;

                Heading(headingLevel, "Type Parameters");
                body.Add(new Parameters { parameters = typeParameters.Select(ToParameter).ToArray() });

                Parameter ToParameter(ITypeParameterSymbol param)
                {
                    var docs = comment?.TypeParameters is { } p && p.TryGetValue(param.Name, out var value) ? value : null;
                    return new() { name = param.Name, description = docs };
                }
            }

            void Method(IMethodSymbol symbol, Compilation compilation, int headingLevel)
            {
                Api(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), symbol, compilation);

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
                Api(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), symbol, compilation);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                Heading(headingLevel + 1, "Field Value");
                body.Add(new Parameters
                {
                    parameters =
                    [
                        new Parameter() { type = FullLink(symbol.Type, compilation) }
                    ]
                });

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void Property(IPropertySymbol symbol, Compilation compilation, int headingLevel)
            {
                Api(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), symbol, compilation);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                Heading(headingLevel + 1, "Property Value");
                body.Add(new Parameters
                {
                    parameters =
                    [
                        new Parameter() { type = FullLink(symbol.Type, compilation) }
                    ]
                });

                Examples(comment, headingLevel + 1);
                Remarks(comment, headingLevel + 1);
                Exceptions(comment, headingLevel + 1);
                SeeAlsos(comment, headingLevel + 1);
            }

            void Event(IEventSymbol symbol, Compilation compilation, int headingLevel)
            {
                Api(headingLevel, SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp), symbol, compilation);

                var comment = Comment(symbol, compilation);
                Summary(comment);
                Syntax(symbol);

                Heading(headingLevel + 1, "Event Type");
                body.Add(new Parameters
                {
                    parameters =
                    [
                        new Parameter() { type = FullLink(symbol.Type, compilation) }
                    ]
                });

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
                    items = items.OrderBy(static m => m.Name).ToList();

                body.Add((Heading)new H2 { h2 = "Fields" });
                body.Add(new Parameters { parameters = items.Select(ToParameter).ToArray() });

                return;

                Parameter ToParameter(IFieldSymbol item)
                {
                    var docs = Comment(item, compilation) is { } comment ? string.Join("\n\n", comment.Summary, comment.Remarks) : null;

                    return new()
                    {
                        name = item.Name,
                        @default = $"{item.ConstantValue}",
                        deprecated = Deprecated(item),
                        preview = Preview(item),
                        description = docs,
                    };
                }
            }

            IEnumerable<Fact> Facts()
            {
                yield return new("Namespace", ShortLink(symbol.ContainingNamespace, compilation));

                var assemblies = symbols.Select(s => s.symbol.ContainingAssembly.Name).Where(n => n != "?").Distinct().Select(n => $"{n}.dll").ToList();
                if (assemblies.Count > 0)
                    yield return new("Assembly", (Span)string.Join(", ", assemblies));
            }

            void Summary(XmlComment? comment)
            {
                if (!string.IsNullOrEmpty(comment?.Summary))
                    body.Add(new Markdown { markdown = comment.Summary });
            }

            void Syntax(ISymbol symbol)
            {
                var syntax = SymbolFormatter.GetSyntax(symbol, SyntaxLanguage.CSharp, filter);
                body.Add(new Code { code = syntax });
            }

            void Examples(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.Examples?.Count > 0)
                {
                    Heading(headingLevel, "Examples");

                    foreach (var example in comment.Examples)
                        body.Add(new Markdown { markdown = example });
                }
            }

            void Remarks(XmlComment? comment, int headingLevel = 2)
            {
                if (!string.IsNullOrEmpty(comment?.Remarks))
                {
                    Heading(headingLevel, "Remarks");
                    body.Add(new Markdown { markdown = comment.Remarks });
                }
            }

            void Exceptions(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.Exceptions?.Count > 0)
                {
                    Heading(headingLevel, "Exceptions");
                    body.Add(new Parameters
                    {
                        parameters = comment.Exceptions.Select(e => new Parameter()
                        {
                            type = Cref(e.CommentId),
                            description = e.Description,
                        }).ToArray()
                    });
                }
            }

            void SeeAlsos(XmlComment? comment, int headingLevel = 2)
            {
                if (comment?.SeeAlsos?.Count > 0)
                {
                    Heading(headingLevel, "See Also");
                    body.Add(new List
                    {
                        list = comment.SeeAlsos.Select(s => s.LinkType switch
                        {
                            LinkType.CRef => Cref(s.CommentId),
                            LinkType.HRef => Link(s.LinkId, s.LinkId),
                            _ => throw new NotSupportedException($"{s.LinkType}"),
                        }).ToArray()
                    });
                }
            }

            Inline Cref(string commentId)
            {
                return DocumentationCommentId.GetFirstSymbolForDeclarationId(commentId, compilation) is { } symbol ? FullLink(symbol, compilation) : Array.Empty<Span>();
            }
        }

        Inline ShortLink(ISymbol symbol, Compilation compilation)
        {
            var title = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
            var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, symbolUrlKind, allAssemblies, filter);
            return Link(title, url);
        }

        Inline FullLink(ISymbol symbol, Compilation compilation)
        {
            var parts = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp);
            var linkItems = SymbolFormatter.ToLinkItems(parts, compilation, config.MemberLayout, allAssemblies, overload: false, filter, symbolUrlKind);

            return linkItems.Select(i => Link(i.DisplayName, i.Href)).ToArray();
        }

        Inline NameOnlyLink(ISymbol symbol, Compilation compilation)
        {
            var title = SymbolFormatter.GetName(symbol, SyntaxLanguage.CSharp);
            var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, symbolUrlKind, allAssemblies, filter);
            return Link(title, url);
        }

        Span Link(string text, string? url)
        {
            return string.IsNullOrEmpty(url) ? text : new LinkSpan { text = text, url = url };
        }

        XmlComment Comment(ISymbol symbol, Compilation compilation)
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

    static string HtmlInnerText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc.DocumentNode.InnerText;
    }
}
