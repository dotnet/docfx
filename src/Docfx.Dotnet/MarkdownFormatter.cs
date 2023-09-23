// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.ManagedReference;
using Docfx.Plugins;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

#nullable enable

namespace Docfx.Dotnet;

static class MarkdownFormatter
{
    public static void Save(List<(IAssemblySymbol symbol, Compilation compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Logger.LogWarning($"Markdown output format is experimental.");

        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.symbol.FindExtensionMethods(filter)).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.symbol), SymbolEqualityComparer.Default);
        var allTypes = assemblies.SelectMany(a => a.symbol.GetAllTypes(filter).Select(t => (symbol: t, a.compilation))).ToList();

        var allNamespaces = allTypes
            .Select(t => (symbol: t.symbol.ContainingNamespace, t.compilation))
            .DistinctBy(n => n.symbol, SymbolEqualityComparer.Default)
            .GroupBy(n => VisitorHelper.GetId(n.symbol))
            .ToDictionary(g => g.Key, g => g.ToList());

        Parallel.ForEach(allTypes, i => SaveNamedType(i.symbol, i.compilation));
        Parallel.ForEach(allNamespaces, i => SaveNamespace(i.Key, i.Value));

        SaveToc();

        Logger.LogInfo($"Export succeed: {allTypes.Count} types and {allNamespaces.Count} namespaces.");

        void SaveNamespace(string id, List<(INamespaceSymbol symbol, Compilation compilation)> namespaces)
        {
            var ns = namespaces[0].symbol;
            var compilation = namespaces[0].compilation;
            var namespaceSymbols = namespaces.Select(n => n.symbol).ToHashSet(SymbolEqualityComparer.Default);
            var types = allTypes.Where(t => namespaceSymbols.Contains(t.symbol.ContainingNamespace)).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"# Namespace {Escape(ns.ToString()!)}").AppendLine();

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
                var comment = Comment(ns, compilation);
                if (!string.IsNullOrEmpty(comment?.Summary))
                    sb.AppendLine(comment.Summary).AppendLine();
            }

            void Namespaces()
            {
                var items = namespaces
                    .SelectMany(n => n.symbol.GetNamespaceMembers().Select(symbol => (symbol, n.compilation)))
                    .DistinctBy(n => n.symbol.Name)
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
                var items = allTypes
                    .Select(t => t.symbol)
                    .Where(t => SymbolEqualityComparer.Default.Equals(t.BaseType, symbol))
                    .OrderBy(t => t.Name).ToList();

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

        void SaveToc()
        {
            var toc = new TocViewModel();
            var tocNodeByNamespace = new Dictionary<string, TocItemViewModel>();

            foreach (var (symbol, _) in allTypes)
            {
                var ns = symbol.ContainingNamespace.ToString()!;
                if (!tocNodeByNamespace.TryGetValue(ns, out var namespaceTocNode))
                {
                    namespaceTocNode = new()
                    {
                        Name = ns,
                        Href = $"{VisitorHelper.GetId(symbol.ContainingNamespace)}.md",
                        Items = new(),
                    };
                    tocNodeByNamespace.Add(ns, namespaceTocNode);
                    toc.Add(namespaceTocNode);
                }
                namespaceTocNode.Items.Add(new() { Name = symbol.Name, Href = $"{VisitorHelper.GetId(symbol)}.md" });
            }

            SortTocItems(toc);
            YamlUtility.Serialize(Path.Combine(config.OutputFolder, "toc.yml"), toc, YamlMime.TableOfContent);

            static void SortTocItems(TocViewModel node)
            {
                node.Sort((a, b) => a.Name.CompareTo(b.Name));

                foreach (var child in node)
                {
                    if (child.Items is not null)
                        SortTocItems(child.Items);
                }
            }
        }

        string Link(ISymbol symbol, Compilation compilation)
        {
            return symbol.Kind is SymbolKind.Method or SymbolKind.Namespace ? ShortLink() : FullLink();

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
