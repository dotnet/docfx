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
    public static void Save(List<(IAssemblySymbol, Compilation)> assemblies, ExtractMetadataConfig config, DotnetApiOptions options)
    {
        Logger.LogWarning($"Markdown output format is experimental.");

        Directory.CreateDirectory(config.OutputFolder);

        var filter = new SymbolFilter(config, options);
        var extensionMethods = assemblies.SelectMany(assembly => assembly.Item1.FindExtensionMethods()).Where(filter.IncludeApi).ToArray();
        var allAssemblies = new HashSet<IAssemblySymbol>(assemblies.Select(a => a.Item1), SymbolEqualityComparer.Default);
        var allTypes = allAssemblies.SelectMany(a => a.GlobalNamespace.GetAllTypes(default).Where(filter.IncludeApi)).ToArray();

        foreach (var (assembly, compilation) in assemblies)
        {
            SaveCore(assembly, compilation);
        }

        void SaveCore(IAssemblySymbol assembly, Compilation compilation)
        {
            Logger.LogInfo($"Processing {assembly.Name}");
            VisitNamespace(assembly.GlobalNamespace);

            void VisitNamespace(INamespaceSymbol symbol)
            {
                if (!filter.IncludeApi(symbol))
                    return;

                foreach (var ns in symbol.GetNamespaceMembers())
                    VisitNamespace(ns);

                foreach (var type in symbol.GetTypeMembers())
                    VisitNamedType(type);
            }

            void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (!filter.IncludeApi(symbol))
                    return;

                foreach (var subtype in symbol.GetTypeMembers())
                    VisitNamedType(subtype);

                Save(symbol);
            }

            void Save(INamedTypeSymbol symbol)
            {
                var sb = new StringBuilder();
                var comment = Comment(symbol);

                switch (symbol.TypeKind)
                {
                    case TypeKind.Enum: Enum(); break;
                    case TypeKind.Delegate: Delegate(); break;
                    case TypeKind.Interface or TypeKind.Structure or TypeKind.Class: Class(); break;
                }

                var filename = Path.Combine(config.OutputFolder, VisitorHelper.GetId(symbol) + ".md");
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
                    sb.AppendLine(string.Join(" \u2190 ", items.Select(i => "\n" + FullLink(i)))).AppendLine();
                }

                void Derived()
                {
                    var items = allTypes
                        .Where(t => SymbolEqualityComparer.Default.Equals(t.BaseType, symbol))
                        .OrderBy(t => t.Name).ToList();

                    if (items.Count is 0)
                        return;

                    sb.AppendLine($"#### Derived");
                    sb.AppendLine(string.Join(", ", items.Select(i => "\n" + FullLink(i)))).AppendLine();
                }

                void Implements()
                {
                    var items = symbol.AllInterfaces.Where(filter.IncludeApi).ToList();
                    if (items.Count is 0)
                        return;

                    sb.AppendLine($"#### Implements");
                    sb.AppendLine(string.Join(", ", items.Select(i => "\n" + FullLink(i)))).AppendLine();
                }

                void InheritedMembers()
                {
                    var items = symbol.GetInheritedMembers(filter).ToList();
                    if (items.Count is 0)
                        return;

                    // TODO:
                    sb.AppendLine($"#### Inherited Members");
                    sb.AppendLine(string.Join(", ", items.Select(i => "\n" + ShortLink(i)))).AppendLine();
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
                    sb.AppendLine(string.Join(", ", items.Select(i => "\n" + ShortLink(i)))).AppendLine();
                }

                void Parameters(ISymbol symbol, XmlComment? comment, string heading = "##")
                {
                    var parameters = symbol.GetParameters();
                    if (!parameters.Any())
                        return;

                    sb.AppendLine($"{heading} Parameters").AppendLine();

                    foreach (var param in parameters)
                    {
                        sb.AppendLine($"`{Escape(param.Name)}` {FullLink(param.Type)}").AppendLine();

                        if (comment?.Parameters?.TryGetValue(param.Name, out var value) ?? false)
                            sb.AppendLine($"{value}").AppendLine();
                    }
                }

                void Returns(IMethodSymbol symbol, XmlComment? comment, string heading = "##")
                {
                    if (symbol.ReturnType is null || symbol.ReturnType.SpecialType is SpecialType.System_Void)
                        return;

                    sb.AppendLine($"{heading} Returns").AppendLine();
                    sb.AppendLine(FullLink(symbol.ReturnType)).AppendLine();

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

                        var comment = Comment(symbol);
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

                        var comment = Comment(symbol);
                        Summary(comment);
                        Syntax(symbol);

                        sb.AppendLine("Field Value").AppendLine();
                        sb.AppendLine(FullLink(symbol.Type)).AppendLine();

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

                        var comment = Comment(symbol);
                        Summary(comment);
                        Syntax(symbol);

                        sb.AppendLine("Property Value").AppendLine();
                        sb.AppendLine(FullLink(symbol.Type)).AppendLine();

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

                        var comment = Comment(symbol);
                        Summary(comment);
                        Syntax(symbol);

                        sb.AppendLine("Event Type").AppendLine();
                        sb.AppendLine(FullLink(symbol.Type)).AppendLine();

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

                        if (Comment(item) is { } comment)
                            sb.AppendLine($"{Escape(comment.Summary)}").AppendLine();
                    }
                }

                void Info()
                {
                    sb.AppendLine($"__Namespace:__ {FullLink(symbol.ContainingNamespace)}  ");
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
            }

            string FullLink(ISymbol symbol)
            {
                var parts = SymbolFormatter.GetNameWithTypeParts(symbol, SyntaxLanguage.CSharp);
                var linkItems = SymbolFormatter.ToLinkItems(parts, compilation, config.MemberLayout, allAssemblies, overload: false, SymbolUrlKind.Markdown);

                return string.Concat(linkItems.Select(i =>
                    string.IsNullOrEmpty(i.Href) ? Escape(i.DisplayName) : $"[{Escape(i.DisplayName)}]({Escape(i.Href)})"));
            }

            string ShortLink(ISymbol symbol)
            {
                var title = SymbolFormatter.GetNameWithType(symbol, SyntaxLanguage.CSharp);
                var url = SymbolUrlResolver.GetSymbolUrl(symbol, compilation, config.MemberLayout, SymbolUrlKind.Markdown, allAssemblies);
                return string.IsNullOrEmpty(url) ? Escape(title) : $"[{Escape(title)}]({Escape(url)})";
            }

            string Cref(string commentId)
            {
                return DocumentationCommentId.GetFirstSymbolForDeclarationId(commentId, compilation) is { } symbol ? FullLink(symbol) : "";
            }

            XmlComment? Comment(ISymbol symbol)
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
        }

        static string Escape(string text)
        {
            return text;
        }
    }
}
