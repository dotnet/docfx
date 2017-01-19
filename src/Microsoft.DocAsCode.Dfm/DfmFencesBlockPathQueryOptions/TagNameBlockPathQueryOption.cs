﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class TagNameBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        public string TagName { get; set; }

        // C# code snippet comment block: // <[/]snippetname>
        private static readonly Regex CSharpCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CSharpCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // C# code snippet region block: start -> #region snippetname, end -> #endregion
        private static readonly Regex CSharpCodeSnippetRegionStartLineRegex = new Regex(@"^\s*#\s*region\s+(?<name>.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex CSharpCodeSnippetRegionEndLineRegex = new Regex(@"^\s*#\s*endregion(?:\s.*)?$", RegexOptions.Compiled);

        // VB code snippet comment block: ' <[/]snippetname>
        private static readonly Regex VBCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\'\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VBCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\'\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
        private static readonly Regex VBCodeSnippetRegionRegionStartLineRegex = new Regex(@"^\s*#\s*Region\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex VBCodeSnippetRegionRegionEndLineRegex = new Regex(@"^\s*#\s*End\s+Region(?:\s.*)?$", RegexOptions.Compiled);

        // C++ code snippet block: // <[/]snippetname>
        private static readonly Regex CPlusPlusCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CPlusPlusCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // F# code snippet block: // <[/]snippetname>
        private static readonly Regex FSharpCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FSharpCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XmlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex XmlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XAML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XamlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex XamlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // HTML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex HtmlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Sql code snippet block: -- <[/]snippetname>
        private static readonly Regex SqlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SqlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Javascript code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex JavaScriptSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex JavaScriptSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Language names and aliases fllow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
        // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
        // Currently only supports parts of the language names, aliases and extensions
        // Later we can move the repository's supported/custom language names, aliases, extensions and corresponding comments regexes to docfx build configuration
        private static readonly IReadOnlyDictionary<string, List<ICodeSnippetExtractor>> CodeLanguageExtractors =
            new Dictionary<string, List<ICodeSnippetExtractor>>(StringComparer.OrdinalIgnoreCase)
            {
                [".cs"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CSharpCodeSnippetCommentStartLineRegex, CSharpCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(CSharpCodeSnippetRegionStartLineRegex, CSharpCodeSnippetRegionEndLineRegex)
                },
                ["cs"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CSharpCodeSnippetCommentStartLineRegex, CSharpCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(CSharpCodeSnippetRegionStartLineRegex, CSharpCodeSnippetRegionEndLineRegex)
                },
                ["csharp"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CSharpCodeSnippetCommentStartLineRegex, CSharpCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(CSharpCodeSnippetRegionStartLineRegex, CSharpCodeSnippetRegionEndLineRegex)
                },
                [".vb"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(VBCodeSnippetCommentStartLineRegex, VBCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(VBCodeSnippetRegionRegionStartLineRegex, VBCodeSnippetRegionRegionEndLineRegex)
                },
                ["vb"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(VBCodeSnippetCommentStartLineRegex, VBCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(VBCodeSnippetRegionRegionStartLineRegex, VBCodeSnippetRegionRegionEndLineRegex)
                },
                ["vbnet"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(VBCodeSnippetCommentStartLineRegex, VBCodeSnippetCommentEndLineRegex),
                    new RecursiveNameCodeSnippetExtractor(VBCodeSnippetRegionRegionStartLineRegex, VBCodeSnippetRegionRegionEndLineRegex)
                },
                [".cpp"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                [".h"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                [".hpp"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                [".c"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                [".cc"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                ["cpp"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                ["c++"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(CPlusPlusCodeSnippetCommentStartLineRegex, CPlusPlusCodeSnippetCommentEndLineRegex)
                },
                ["fs"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(FSharpCodeSnippetCommentStartLineRegex, FSharpCodeSnippetCommentEndLineRegex)
                },
                ["fsharp"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(FSharpCodeSnippetCommentStartLineRegex, FSharpCodeSnippetCommentEndLineRegex)
                },
                [".fs"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(FSharpCodeSnippetCommentStartLineRegex, FSharpCodeSnippetCommentEndLineRegex)
                },
                [".fsi"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(FSharpCodeSnippetCommentStartLineRegex, FSharpCodeSnippetCommentEndLineRegex)
                },
                [".fsx"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(FSharpCodeSnippetCommentStartLineRegex, FSharpCodeSnippetCommentEndLineRegex)
                },
                [".xml"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(XmlCodeSnippetCommentStartLineRegex, XmlCodeSnippetCommentEndLineRegex)
                },
                [".csdl"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(XmlCodeSnippetCommentStartLineRegex, XmlCodeSnippetCommentEndLineRegex)
                },
                [".edmx"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(XmlCodeSnippetCommentStartLineRegex, XmlCodeSnippetCommentEndLineRegex)
                },
                ["xml"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(XmlCodeSnippetCommentStartLineRegex, XmlCodeSnippetCommentEndLineRegex)
                },
                [".html"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(HtmlCodeSnippetCommentStartLineRegex, HtmlCodeSnippetCommentEndLineRegex)
                },
                ["html"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(HtmlCodeSnippetCommentStartLineRegex, HtmlCodeSnippetCommentEndLineRegex)
                },
                [".xaml"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(XamlCodeSnippetCommentStartLineRegex, XamlCodeSnippetCommentEndLineRegex)
                },
                [".sql"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(SqlCodeSnippetCommentStartLineRegex, SqlCodeSnippetCommentEndLineRegex)
                },
                ["sql"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(SqlCodeSnippetCommentStartLineRegex, SqlCodeSnippetCommentEndLineRegex)
                },
                [".js"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(JavaScriptSnippetCommentStartLineRegex, JavaScriptSnippetCommentEndLineRegex)
                },
                ["js"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(JavaScriptSnippetCommentStartLineRegex, JavaScriptSnippetCommentEndLineRegex)
                },
                ["javascript"] = new List<ICodeSnippetExtractor>
                {
                    new FlatNameCodeSnippetExtractor(JavaScriptSnippetCommentStartLineRegex, JavaScriptSnippetCommentEndLineRegex)
                }
            };

        private DfmTagNameResolveResult resolveResult;

        private readonly ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>> _dfmTagNameLineRangeCache
            = new ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>>(StringComparer.OrdinalIgnoreCase);

        public override bool ValidateAndPrepare(string[] lines, DfmFencesToken token)
        {
            // NOTE: Parsing language and removing comment lines only do for tag name representation
            var lang = GetCodeLanguageOrExtension(token);
            List<ICodeSnippetExtractor> extractors;
            if (!CodeLanguageExtractors.TryGetValue(lang, out extractors))
            {
                ErrorMessage = $"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead";
                return false;
            }

            resolveResult = ResolveTagNamesFromPath(token.Path, lines, TagName, extractors);
            if (!resolveResult.IsSuccessful)
            {
                ErrorMessage = resolveResult.ErrorMessage;
                return false;
            }

            return true;
        }

        public override IEnumerable<string> GetQueryLines(string[] lines)
        {
            for (int i = resolveResult.StartLine; i <= Math.Min(resolveResult.EndLine, lines.Length); i++)
            {
                if (resolveResult.ExcludesLines == null || !resolveResult.ExcludesLines.Contains(i))
                {
                    yield return lines[i - 1];
                }
            }
        }

        private DfmTagNameResolveResult ResolveTagNamesFromPath(string fencesPath, string[] fencesCodeLines, string tagName, List<ICodeSnippetExtractor> codeSnippetExtractors)
        {
            var lazyResolveResults =
                _dfmTagNameLineRangeCache.GetOrAdd(fencesPath,
                    path => new Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>(
                            () =>
                            {
                                // TODO: consider different code snippet representation with same name
                                return new ConcurrentDictionary<string, DfmTagNameResolveResult>(
                                    (from codeSnippetExtractor in codeSnippetExtractors
                                     let result = codeSnippetExtractor.GetAll(fencesCodeLines)
                                     from codeSnippet in result
                                     group codeSnippet by codeSnippet.Key).ToDictionary(d => d.Key, d => d.First().Value), StringComparer.OrdinalIgnoreCase);
                            }));

            DfmTagNameResolveResult resolveResult;
            var tagNamesDictionary = lazyResolveResults.Value;
            return (tagNamesDictionary.TryGetValue(tagName, out resolveResult) || tagNamesDictionary.TryGetValue($"snippet{tagName}", out resolveResult))
                    ? resolveResult
                    : new DfmTagNameResolveResult { IsSuccessful = false, ErrorMessage = $"Tag name {tagName} is not found" };
        }


        private static string GetCodeLanguageOrExtension(DfmFencesToken token)
        {
            return !string.IsNullOrEmpty(token.Lang) ? token.Lang : Path.GetExtension(token.Path);
        }
    }
}
