// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    internal class DfmCodeExtractor
    {
        // C# code snippet comment block: // <[/]snippetname>
        private static readonly Regex CSharpCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CSharpCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // C# code snippet region block: start -> #region snippetname, end -> #endregion
        private static readonly Regex CSharpCodeSnippetRegionStartLineRegex = new Regex(@"^\s*#\s*region\s+(?<name>.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex CSharpCodeSnippetRegionEndLineRegex = new Regex(@"^\s*#\s*endregion\s*$", RegexOptions.Compiled);

        // VB code snippet comment block: ' <[/]snippetname>
        private static readonly Regex VBCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\'\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VBCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\'\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
        private static readonly Regex VBCodeSnippetRegionRegionStartLineRegex = new Regex(@"^\s*#\s*Region\s*(?<name>.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex VBCodeSnippetRegionRegionEndLineRegex = new Regex(@"^\s*#\s*End\s+Region\s*$", RegexOptions.Compiled);

        // C++ code snippet block: // <[/]snippetname>
        private static readonly Regex CPlusPlusCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CPlusPlusCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // F# code snippet block: // <[/]snippetname>
        private static readonly Regex FSharpCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex FSharpCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XmlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex XmlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XAML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XamlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex XamlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // HTML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex HtmlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HtmlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Sql code snippet block: -- <[/]snippetname>
        private static readonly Regex SqlCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SqlCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Javascript code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex JavaScriptSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex JavaScriptSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string RemoveIndentSpacesRegexString = @"^[ \t]{{1,{0}}}";

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

        private readonly ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>> _dfmTagNameLineRangeCache
            = new ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>>(StringComparer.OrdinalIgnoreCase);

        public DfmExtractCodeResult ExtractFencesCode(DfmFencesBlockToken token, string fencesPath)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrEmpty(fencesPath))
            {
                throw new ArgumentNullException(nameof(fencesPath));
            }

            var fencesCode = File.ReadAllLines(fencesPath);

            // NOTE: Parsing language and removing comment lines only do for tag name representation
            if (token.PathQueryOption?.TagName != null)
            {
                var lang = GetCodeLanguageOrExtension(token);
                List<ICodeSnippetExtractor> extractors;
                if (!CodeLanguageExtractors.TryGetValue(lang, out extractors))
                {
                    string errorMessage = $"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead";
                    Logger.LogError(errorMessage);
                    return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = errorMessage, FencesCodeLines = fencesCode };
                }

                var resolveResult = ResolveTagNamesFromPath(fencesPath, fencesCode, token.PathQueryOption.TagName, extractors);
                if (!resolveResult.IsSuccessful)
                {
                    Logger.LogError(resolveResult.ErrorMessage);
                    return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = resolveResult.ErrorMessage, FencesCodeLines = fencesCode };
                }

                return GetFencesCodeCore(fencesCode, resolveResult.StartLine, resolveResult.EndLine, resolveResult.IndentLength, resolveResult.ExcludesLines);
            }
            else
            {
                // line range check only need to be done for line number representation
                string errorMessage;
                if (!CheckLineRange(fencesCode.Length, token.PathQueryOption?.StartLine, token.PathQueryOption?.EndLine, out errorMessage))
                {
                    Logger.LogError(errorMessage);
                    return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = errorMessage, FencesCodeLines = fencesCode };
                }

                int startLine = token.PathQueryOption?.StartLine ?? 1;
                int endLine = token.PathQueryOption?.EndLine ?? fencesCode.Length;
                int indentLength = (from line in fencesCode.Skip(startLine - 1).Take(endLine - startLine + 1)
                                    where !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)
                                    select (int?)DfmCodeExtractorHelper.GetIndentLength(line)).Min() ?? 0;
                return GetFencesCodeCore(fencesCode, startLine, endLine, indentLength);
            }
        }

        private DfmExtractCodeResult GetFencesCodeCore(string[] codeLines, int startLine, int endLine, int indentLength, HashSet<int> excludedLines = null)
        {
            long totalLines = codeLines.Length;
            var includedLines = new List<string>();
            for (int i = startLine; i <= Math.Min(endLine, totalLines); i++)
            {
                if (excludedLines == null || !excludedLines.Contains(i))
                {
                    includedLines.Add(codeLines[i - 1]);
                }
            }

            return new DfmExtractCodeResult
            {
                IsSuccessful = true,
                FencesCodeLines = (indentLength == 0 ? includedLines : includedLines.Select(s => Regex.Replace(s, string.Format(RemoveIndentSpacesRegexString, indentLength), string.Empty))).ToArray()
            };
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

        private static bool CheckLineRange(int totalLines, int? startLine, int? endLine, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (startLine <= 0 || endLine <= 0)
            {
                errorMessage = "Start/End line should be larger than zero";
                return false;
            }

            if (startLine > endLine)
            {
                errorMessage = $"Start line {startLine} shouldn't be larger than end line {endLine}";
                return false;
            }

            if (startLine > totalLines)
            {
                errorMessage = $"Start line '{startLine}' execeeds total file lines '{totalLines}'";
                return false;
            }

            return true;
        }

        private static string GetCodeLanguageOrExtension(DfmFencesBlockToken token)
        {
            return !string.IsNullOrEmpty(token.Lang) ? token.Lang : Path.GetExtension(token.Path);
        }
    }
}