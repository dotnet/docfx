// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;

    public class TagNameBlockPathQueryOption : DfmFencesBlockPathQueryOption
    {
        // C family code snippet comment block: // <[/]snippetname>
        private static readonly Regex CFamilyCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CFamilyCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\/{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Basic family code snippet comment block: ' <[/]snippetname>
        private static readonly Regex BasicFamilyCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\'\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BasicFamilyCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\'\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Markup language family code snippet block: <!-- <[/]snippetname> -->
        private static readonly Regex MarkupLanguageFamilyCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MarkupLanguageFamilyCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\<\!\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*\-{2}\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Sql family code snippet block: -- <[/]snippetname>
        private static readonly Regex SqlFamilyCodeSnippetCommentStartLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SqlFamilyCodeSnippetCommentEndLineRegex = new Regex(@"^\s*\-{2}\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Script family snippet comment block: # <[/]snippetname>
        private static readonly Regex ScriptFamilyCodeSnippetCommentStartLineRegex = new Regex(@"^\s*#\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScriptFamilyCodeSnippetCommentEndLineRegex = new Regex(@"^\s*#\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Batch code snippet comment block: rem <[/]snippetname>
        private static readonly Regex BatchFileCodeSnippetRegionStartLineRegex = new Regex(@"^\s*rem\s+\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BatchFileCodeSnippetRegionEndLineRegex = new Regex(@"^\s*rem\s+\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // C# code snippet region block: start -> #region snippetname, end -> #endregion
        private static readonly Regex CSharpCodeSnippetRegionStartLineRegex = new Regex(@"^\s*#\s*region(?:\s+(?<name>.+?))?\s*$", RegexOptions.Compiled);
        private static readonly Regex CSharpCodeSnippetRegionEndLineRegex = new Regex(@"^\s*#\s*endregion(?:\s.*)?$", RegexOptions.Compiled);

        // Erlang code snippet comment block: % <[/]snippetname>
        private static readonly Regex ErlangCodeSnippetRegionStartLineRegex = new Regex(@"^\s*%\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ErlangCodeSnippetRegionEndLineRegex = new Regex(@"^\s*%\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Lisp code snippet comment block: ; <[/]snippetname>
        private static readonly Regex LispCodeSnippetRegionStartLineRegex = new Regex(@"^\s*;\s*\<\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LispCodeSnippetRegionEndLineRegex = new Regex(@"^\s*;\s*\<\s*\/\s*(?<name>[\w\.]+)\s*\>\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
        private static readonly Regex VBCodeSnippetRegionRegionStartLineRegex = new Regex(@"^\s*#\s*Region(?:\s+(?<name>.+?))?\s*$", RegexOptions.Compiled);
        private static readonly Regex VBCodeSnippetRegionRegionEndLineRegex = new Regex(@"^\s*#\s*End\s+Region(?:\s.*)?$", RegexOptions.Compiled);

        // Language names and aliases fllow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
        // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
        // Currently only supports parts of the language names, aliases and extensions
        // Later we can move the repository's supported/custom language names, aliases, extensions and corresponding comments regexes to docfx build configuration
        private readonly IReadOnlyDictionary<string, List<ICodeSnippetExtractor>> _codeLanguageExtractors;

        public string TagName { get; set; }

        private readonly bool _noCache;

        private DfmTagNameResolveResult _resolveResult;

        public TagNameBlockPathQueryOption()
            : this(false) { }

        public TagNameBlockPathQueryOption(bool noCache = false)
            : this(null, noCache) { }

        public TagNameBlockPathQueryOption(CodeLanguageExtractorsBuilder codeLanguageExtractors, bool noCache)
        {
            _codeLanguageExtractors = (codeLanguageExtractors ?? GetDefaultCodeLanguageExtractorsBuilder()).ToDictionay() ;
            _noCache = noCache;
        }

        private readonly ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, List<DfmTagNameResolveResult>>>> _dfmTagNameLineRangeCache =
            new ConcurrentDictionary<string, Lazy<ConcurrentDictionary<string, List<DfmTagNameResolveResult>>>>(StringComparer.OrdinalIgnoreCase);

        public static CodeLanguageExtractorsBuilder GetDefaultCodeLanguageExtractorsBuilder()
        {
            return new CodeLanguageExtractorsBuilder()
                .AddAlias("actionscript", ".as")
                .AddAlias("arduino", ".ino")
                .AddAlias("assembly", "nasm", ".asm")
                .AddAlias("batchfile", ".bat", ".cmd")
                .AddAlias("cpp", "c", "c++", "objective-c", "obj-c", "objc", "objectivec", ".c", ".cpp", ".h", ".hpp", ".cc")
                .AddAlias("csharp", "cs", ".cs")
                .AddAlias("cuda", ".cu", ".cuh")
                .AddAlias("d", "dlang", ".d")
                .AddAlias("erlang", ".erl")
                .AddAlias("fsharp", "fs", ".fs", ".fsi", ".fsx")
                .AddAlias("go", "golang", ".go")
                .AddAlias("haskell", ".hs")
                .AddAlias("html", ".html", ".jsp", ".asp", ".aspx", ".ascx")
                .AddAlias("cshtml", ".cshtml", "aspx-cs", "aspx-csharp")
                .AddAlias("vbhtml", ".vbhtml", "aspx-vb")
                .AddAlias("java", ".java")
                .AddAlias("javascript", "js", "node", ".js")
                .AddAlias("lisp", ".lisp", ".lsp")
                .AddAlias("lua", ".lua")
                .AddAlias("matlab", ".matlab")
                .AddAlias("pascal", ".pas")
                .AddAlias("perl", ".pl")
                .AddAlias("php", ".php")
                .AddAlias("powershell", "posh", ".ps1")
                .AddAlias("processing", ".pde")
                .AddAlias("python", ".py")
                .AddAlias("r", ".r")
                .AddAlias("ruby", "ru", ".ru", ".ruby")
                .AddAlias("rust", ".rs")
                .AddAlias("scala", ".scala")
                .AddAlias("shell", "sh", "bash", ".sh", ".bash")
                .AddAlias("smalltalk", ".st")
                .AddAlias("sql", ".sql")
                .AddAlias("swift", ".swift")
                .AddAlias("typescript", "ts", ".ts")
                .AddAlias("xaml", ".xaml")
                .AddAlias("xml", "xsl", "xslt", "xsd", "wsdl", ".xml", ".csdl", ".edmx", ".xsl", ".xslt", ".xsd", ".wsdl")
                .AddAlias("vb", "vbnet", "vbscript", ".vb", ".bas", ".vbs", ".vba")
                // family
                .Add(
                    new FlatNameCodeSnippetExtractor(BasicFamilyCodeSnippetCommentStartLineRegex, BasicFamilyCodeSnippetCommentEndLineRegex),
                    "vb", "vbhtml")
                .Add(
                    new FlatNameCodeSnippetExtractor(CFamilyCodeSnippetCommentStartLineRegex, CFamilyCodeSnippetCommentEndLineRegex),
                    "actionscript", "arduino", "assembly", "cpp", "csharp", "cshtml", "cuda", "d", "fsharp", "go", "java", "javascript", "pascal", "php", "processing", "rust", "scala", "smalltalk", "swift", "typescript")
                .Add(
                    new FlatNameCodeSnippetExtractor(MarkupLanguageFamilyCodeSnippetCommentStartLineRegex, MarkupLanguageFamilyCodeSnippetCommentEndLineRegex),
                    "xml", "xaml", "html", "cshtml", "vbhtml")
                .Add(
                    new FlatNameCodeSnippetExtractor(SqlFamilyCodeSnippetCommentStartLineRegex, SqlFamilyCodeSnippetCommentEndLineRegex),
                    "haskell", "lua", "sql")
                .Add(
                    new FlatNameCodeSnippetExtractor(ScriptFamilyCodeSnippetCommentStartLineRegex, ScriptFamilyCodeSnippetCommentEndLineRegex),
                    "perl", "powershell", "python", "r", "ruby", "shell")
                // specical language
                .Add(
                    new FlatNameCodeSnippetExtractor(BatchFileCodeSnippetRegionStartLineRegex, BatchFileCodeSnippetRegionEndLineRegex),
                    "batchfile")
                .Add(
                    new RecursiveNameCodeSnippetExtractor(CSharpCodeSnippetRegionStartLineRegex, CSharpCodeSnippetRegionEndLineRegex),
                    "csharp", "cshtml")
                .Add(
                    new FlatNameCodeSnippetExtractor(ErlangCodeSnippetRegionStartLineRegex, ErlangCodeSnippetRegionEndLineRegex),
                    "erlang", "matlab")
                .Add(
                    new FlatNameCodeSnippetExtractor(LispCodeSnippetRegionStartLineRegex, LispCodeSnippetRegionEndLineRegex),
                    "lisp")
                .Add(
                    new RecursiveNameCodeSnippetExtractor(VBCodeSnippetRegionRegionStartLineRegex, VBCodeSnippetRegionRegionEndLineRegex),
                    "vb", "vbhtml");
        }

        public override IEnumerable<string> GetQueryLines(string[] lines, DfmFencesToken token)
        {
            // NOTE: Parsing language and removing comment lines only do for tag name representation
            var lang = GetCodeLanguageOrExtension(token);
            if (!_codeLanguageExtractors.TryGetValue(lang, out List<ICodeSnippetExtractor> extractors))
            {
                throw new DfmCodeExtractorException($"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead");
            }

            _resolveResult = ResolveTagNamesFromPath(token.Path, lines, TagName, extractors);
            if (!string.IsNullOrEmpty(_resolveResult.ErrorMessage))
            {
                Logger.LogWarning(
                    DfmCodeExtractor.GenerateErrorMessage(token, _resolveResult.ErrorMessage),
                    line: token.SourceInfo.LineNumber.ToString());
            }

            var included = new List<string>();
            for (int i = _resolveResult.StartLine; i <= Math.Min(_resolveResult.EndLine, lines.Length); i++)
            {
                if (_resolveResult.ExcludesLines == null || !_resolveResult.ExcludesLines.Contains(i))
                {
                    included.Add(lines[i - 1]);
                }
            }

            return ProcessIncludedLines(included, token);
        }

        private DfmTagNameResolveResult ResolveTagNamesFromPath(string fencesPath, string[] fencesCodeLines, string tagName, List<ICodeSnippetExtractor> codeSnippetExtractors)
        {
            Lazy<ConcurrentDictionary<string, List<DfmTagNameResolveResult>>> lazyResolveResults;
            if (_noCache)
            {
                lazyResolveResults = GetLazyResolveResult(fencesCodeLines, codeSnippetExtractors);
            }
            else
            {
                lazyResolveResults =
                    _dfmTagNameLineRangeCache.GetOrAdd(fencesPath,
                        path => GetLazyResolveResult(fencesCodeLines, codeSnippetExtractors));
            }

            ConcurrentDictionary<string, List<DfmTagNameResolveResult>> tagNamesDictionary;
            try
            {
                tagNamesDictionary = lazyResolveResults.Value;
            }
            catch (Exception e)
            {
                throw new DfmCodeExtractorException($"error resolve tag names from {fencesPath}: {e.Message}", e);
            }
            if (!tagNamesDictionary.TryGetValue(tagName, out List<DfmTagNameResolveResult> results) && !tagNamesDictionary.TryGetValue($"snippet{tagName}", out results))
            {
                throw new DfmCodeExtractorException($"Tag name {tagName} is not found");
            }
            var result = results[0];
            if (results.Count > 1)
            {
                result.ErrorMessage = $"Tag name duplicates at line {string.Join(", ", results.Select(r => r.StartLine))}, the first is chosen. {result.ErrorMessage}";
            }
            return result;
        }

        private Lazy<ConcurrentDictionary<string, List<DfmTagNameResolveResult>>> GetLazyResolveResult(string[] fencesCodeLines, List<ICodeSnippetExtractor> codeSnippetExtractors)
        {
            return new Lazy<ConcurrentDictionary<string, List<DfmTagNameResolveResult>>>(
                 () =>
                 {
                    // TODO: consider different code snippet representation with same name
                    return new ConcurrentDictionary<string, List<DfmTagNameResolveResult>>(
                         (from codeSnippetExtractor in codeSnippetExtractors
                          let resolveResults = codeSnippetExtractor.GetAll(fencesCodeLines)
                          from codeSnippet in resolveResults
                          group codeSnippet by codeSnippet.Key)
                          .ToDictionary(g => g.Key, g => g.Select(p => p.Value).ToList()), StringComparer.OrdinalIgnoreCase);
                 });
        }

        private static string GetCodeLanguageOrExtension(DfmFencesToken token)
        {
            return !string.IsNullOrEmpty(token.Lang) ? token.Lang : Path.GetExtension(token.Path);
        }
    }
}
