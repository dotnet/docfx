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
        // C# code snippet block: // <[/]snippetname>
        private static readonly Regex CSharpCodeSnippetExtractorRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // VB code snippet block: ' <[/]snippetname>
        private static readonly Regex VBCodeSnippetExtractorRegex = new Regex(@"^\s*\'\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // C++ code snippet block: // <[/]snippetname>
        private static readonly Regex CPlusPlusCodeSnippetExtractorRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // F# code snippet block: // <[/]snippetname>
        private static readonly Regex FSharpCodeSnippetExtractorRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XmlCodeSnippetExtractorRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // XAML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex XamlCodeSnippetExtractorRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // HTML code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex HtmlCodeSnippetExtractorRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Sql code snippet block: -- <[/]snippetname>
        private static readonly Regex SqlCodeSnippetExtractorRegex = new Regex(@"^\s*\-{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Javascript code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex JavaScriptSnippetExtractorRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/?\s*([\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string RemoveIndentSpacesRegexString = @"^[ \t]{{1,{0}}}";

        private static readonly List<char> AllowedIndentCharacters = new List<char> { ' ', '\t' };

        // Language names and aliases fllow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
        // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
        // Currently only supports parts of the language names, aliases and extensions
        // Later we can move the repository's supported/custom language names, aliases, extensions and corresponding comments regexes to docfx build configuration
        private static readonly IReadOnlyDictionary<string, Regex> CodeLanguageRegexes =
            new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase)
                {
                    { ".cs", CSharpCodeSnippetExtractorRegex },
                    { "cs", CSharpCodeSnippetExtractorRegex },
                    { "csharp", CSharpCodeSnippetExtractorRegex },
                    { ".vb", VBCodeSnippetExtractorRegex },
                    { "vb", VBCodeSnippetExtractorRegex },
                    { "vbnet", VBCodeSnippetExtractorRegex },
                    { ".cpp", CPlusPlusCodeSnippetExtractorRegex },
                    { ".h", CPlusPlusCodeSnippetExtractorRegex },
                    { ".hpp", CPlusPlusCodeSnippetExtractorRegex },
                    { ".c", CPlusPlusCodeSnippetExtractorRegex },
                    { ".cc", CPlusPlusCodeSnippetExtractorRegex },
                    { "cpp", CPlusPlusCodeSnippetExtractorRegex },
                    { "c++", CPlusPlusCodeSnippetExtractorRegex },
                    { "fs", FSharpCodeSnippetExtractorRegex },
                    { "fsharp", FSharpCodeSnippetExtractorRegex },
                    { ".fs", FSharpCodeSnippetExtractorRegex },
                    { ".fsi", FSharpCodeSnippetExtractorRegex },
                    { ".fsx", FSharpCodeSnippetExtractorRegex },
                    { ".xml", XmlCodeSnippetExtractorRegex },
                    { ".csdl", XmlCodeSnippetExtractorRegex },
                    { ".edmx", XmlCodeSnippetExtractorRegex },
                    { "xml", XmlCodeSnippetExtractorRegex },
                    { ".html", HtmlCodeSnippetExtractorRegex },
                    { "html", HtmlCodeSnippetExtractorRegex },
                    { ".xaml", XamlCodeSnippetExtractorRegex },
                    { ".sql", SqlCodeSnippetExtractorRegex },
                    { "sql", SqlCodeSnippetExtractorRegex },
                    { ".js", JavaScriptSnippetExtractorRegex },
                    { "js", JavaScriptSnippetExtractorRegex },
                    { "javascript", JavaScriptSnippetExtractorRegex },
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
                Regex regex;
                if (!CodeLanguageRegexes.TryGetValue(lang, out regex))
                {
                    string errorMessage = $"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead";
                    Logger.LogError(errorMessage);
                    return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = errorMessage, FencesCodeLines = fencesCode };
                }

                var resolveResult = ResolveTagNamesFromPath(fencesPath, fencesCode, token.PathQueryOption.TagName, regex);
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
                                    select (int?)GetIndentLength(line)).Min() ?? 0;
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

        private DfmTagNameResolveResult ResolveTagNamesFromPath(string fencesPath, string[] fencesCodeLines, string tagName, Regex regexToExtractCode)
        {
            var lazyResolveResults =
                _dfmTagNameLineRangeCache.GetOrAdd(fencesPath,
                    path => new Lazy<ConcurrentDictionary<string, DfmTagNameResolveResult>>(
                            () =>
                            {
                                var linesOfSnippetComment = new Dictionary<int, string>();
                                for (int i = 0; i < fencesCodeLines.Length; i++)
                                {
                                    var match = regexToExtractCode.Match(fencesCodeLines[i]);
                                    if (match.Success)
                                    {
                                        linesOfSnippetComment.Add(i + 1, match.Groups[1].Value);
                                    }
                                }

                                var excludedLines = new HashSet<int>(linesOfSnippetComment.Keys);

                                var dictionary = new ConcurrentDictionary<string, DfmTagNameResolveResult>(StringComparer.OrdinalIgnoreCase);

                                foreach (var snippetCommentsInPair in linesOfSnippetComment.GroupBy(kvp => kvp.Value))
                                {
                                    DfmTagNameResolveResult tagResolveResult;
                                    var lineNumbers = snippetCommentsInPair.Select(line => line.Key).OrderBy(line => line).ToList();
                                    if (lineNumbers.Count == 2)
                                    {
                                        tagResolveResult = new DfmTagNameResolveResult
                                        {
                                            IsSuccessful = true,
                                            StartLine = lineNumbers[0] + 1,
                                            EndLine = lineNumbers[1] - 1,
                                            ExcludesLines = excludedLines,
                                            IndentLength = GetIndentLength(fencesCodeLines[lineNumbers[0]])
                                        };
                                    }
                                    else
                                    {
                                        tagResolveResult = new DfmTagNameResolveResult
                                        {
                                            IsSuccessful = false,
                                            ErrorMessage = lineNumbers.Count == 1
                                                ? $"Tag name {snippetCommentsInPair.Key} is not closed"
                                                : $"Tag name {snippetCommentsInPair.Key} occurs {lineNumbers.Count} times"
                                        };
                                    }

                                    dictionary.TryAdd(snippetCommentsInPair.Key, tagResolveResult);
                                }

                                return dictionary;
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

        private static int GetIndentLength(string s) => s.TakeWhile(c => AllowedIndentCharacters.Contains(c)).Count();

        #region Private class

        private class DfmTagNameResolveResult
        {
            public int StartLine { get; set; }

            public int EndLine { get; set; }

            public int IndentLength { get; set; }

            public HashSet<int> ExcludesLines { get; set; }

            public bool IsSuccessful { get; set; }

            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}