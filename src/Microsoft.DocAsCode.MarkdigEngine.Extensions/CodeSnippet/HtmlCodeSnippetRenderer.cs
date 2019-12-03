// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Markdig.Helpers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class HtmlCodeSnippetRenderer : HtmlObjectRenderer<CodeSnippet>
    {
        private readonly MarkdownContext _context;
        private const string tagPrefix = "snippet";
        private const string warningMessageId = "codeIncludeNotFound";
        private const string defaultWarningMessage = "It looks like the sample you are looking for does not exist.";
        private const string warningTitleId = "warning";
        private const string defaultWarningTitle = "<h5>WARNING</h5>";

        public static readonly IReadOnlyDictionary<string, List<string>> LanguageAlias = new Dictionary<string, List<string>>
        {
            { "actionscript", new List<string>{".as" } },
            { "arduino", new List<string>{".ino" } },
            { "assembly", new List<string>{"nasm", ".asm" } },
            { "batchfile", new List<string>{".bat", ".cmd" } },
            { "cpp", new List<string>{"c", "c++", "objective-c", "obj-c", "objc", "objectivec", ".c", ".cpp", ".h", ".hpp", ".cc" } },
            { "csharp", new List<string>{"cs", ".cs" } },
            { "cuda", new List<string>{".cu", ".cuh" } },
            { "d", new List<string>{"dlang", ".d" } },
            { "erlang", new List<string>{".erl" } },
            { "fsharp", new List<string>{"fs", ".fs", ".fsi", ".fsx" } },
            { "go", new List<string>{"golang", ".go" } },
            { "haskell", new List<string>{".hs" } },
            { "html", new List<string>{".html", ".jsp", ".asp", ".aspx", ".ascx" } },
            { "cshtml", new List<string>{".cshtml", "aspx-cs", "aspx-csharp" } },
            { "vbhtml", new List<string>{".vbhtml", "aspx-vb" } },
            { "java", new List<string>{".java" } },
            { "javascript", new List<string>{"js", "node", ".js" } },
            { "lisp", new List<string>{".lisp", ".lsp" } },
            { "lua", new List<string>{".lua" } },
            { "matlab", new List<string>{".matlab" } },
            { "pascal", new List<string>{".pas" } },
            { "perl", new List<string>{".pl" } },
            { "php", new List<string>{".php" } },
            { "powershell", new List<string>{"posh", ".ps1" } },
            { "processing", new List<string>{".pde" } },
            { "python", new List<string>{".py" } },
            { "r", new List<string>{".r" } },
            { "ruby", new List<string>{"ru", ".ru", ".ruby" } },
            { "rust", new List<string>{".rs" } },
            { "scala", new List<string>{".scala" } },
            { "shell", new List<string>{"sh", "bash", ".sh", ".bash" } },
            { "smalltalk", new List<string>{".st" } },
            { "sql", new List<string>{".sql" } },
            { "swift", new List<string>{".swift" } },
            { "typescript", new List<string>{"ts", ".ts" } },
            { "xaml", new List<string>{".xaml" } },
            { "xml", new List<string>{"xsl", "xslt", "xsd", "wsdl", ".xml", ".csdl", ".edmx", ".xsl", ".xslt", ".xsd", ".wsdl" } },
            { "vb", new List<string>{"vbnet", "vbscript", ".vb", ".bas", ".vbs", ".vba" } }
        };


        // C# code snippet comment block: // <[/]snippetname>
        private static readonly string CFamilyCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string CFamilyCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

        // C# code snippet region block: start -> #region snippetname, end -> #endregion
        private static readonly string CSharpCodeSnippetRegionStartLineTemplate = "#region{tagname}";
        private static readonly string CSharpCodeSnippetRegionEndLineTemplate = "#endregion";

        // VB code snippet comment block: ' <[/]snippetname>
        private static readonly string BasicFamilyCodeSnippetCommentStartLineTemplate = "'<{tagname}>";
        private static readonly string BasicFamilyCodeSnippetCommentEndLineTemplate = "'</{tagname}>";

        // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
        private static readonly string VBCodeSnippetRegionRegionStartLineTemplate = "#region{tagname}";
        private static readonly string VBCodeSnippetRegionRegionEndLineTemplate = "#endregion";

        // XML code snippet block: <!-- <[/]snippetname> -->
        private static readonly string MarkupLanguageFamilyCodeSnippetCommentStartLineTemplate = "<!--<{tagname}>-->";
        private static readonly string MarkupLanguageFamilyCodeSnippetCommentEndLineTemplate = "<!--</{tagname}>-->";

        // Sql code snippet block: -- <[/]snippetname>
        private static readonly string SqlFamilyCodeSnippetCommentStartLineTemplate = "--<{tagname}>";
        private static readonly string SqlFamilyCodeSnippetCommentEndLineTemplate = "--</{tagname}>";

        // Python code snippet comment block: # <[/]snippetname>
        private static readonly string ScriptFamilyCodeSnippetCommentStartLineTemplate = "#<{tagname}>";
        private static readonly string ScriptFamilyCodeSnippetCommentEndLineTemplate = "#</{tagname}>";

        // Batch code snippet comment block: rem <[/]snippetname>
        private static readonly string BatchFileCodeSnippetRegionStartLineTemplate = "rem<{tagname}>";
        private static readonly string BatchFileCodeSnippetRegionEndLineTemplate = "rem</{tagname}>";

        // Erlang code snippet comment block: % <[/]snippetname>
        private static readonly string ErlangCodeSnippetRegionStartLineTemplate = "%<{tagname}>";
        private static readonly string ErlangCodeSnippetRegionEndLineTemplate = "%<{tagname}>";

        // Lisp code snippet comment block: ; <[/]snippetname>
        private static readonly string LispCodeSnippetRegionStartLineTemplate = ";<{tagname}>";
        private static readonly string LispCodeSnippetRegionEndLineTemplate = ";<{tagname}>";

        // Language names and aliases fllow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
        // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
        // Currently only supports parts of the language names, aliases and extensions
        // Later we can move the repository's supported/custom language names, aliases, extensions and corresponding comments regexes to docfx build configuration
        private Dictionary<string, List<CodeSnippetExtrator>> CodeLanguageExtractors = new Dictionary<string, List<CodeSnippetExtrator>>();

        public HtmlCodeSnippetRenderer(MarkdownContext context)
        {
            _context = context;

            BuildCodeLanguageExtractors();
        }

        private void BuildCodeLanguageExtractors()
        {
            AddExtractorItems(new[] { "vb", "vbhtml" },
                new CodeSnippetExtrator(BasicFamilyCodeSnippetCommentStartLineTemplate, BasicFamilyCodeSnippetCommentEndLineTemplate, _context));
            AddExtractorItems(new[] { "actionscript", "arduino", "assembly", "cpp", "csharp", "cshtml", "cuda", "d", "fsharp", "go", "java", "javascript", "pascal", "php", "processing", "rust", "scala", "smalltalk", "swift", "typescript" },
                new CodeSnippetExtrator(CFamilyCodeSnippetCommentStartLineTemplate, CFamilyCodeSnippetCommentEndLineTemplate, _context));
            AddExtractorItems(new[] { "xml", "xaml", "html", "cshtml", "vbhtml" },
                new CodeSnippetExtrator(MarkupLanguageFamilyCodeSnippetCommentStartLineTemplate, MarkupLanguageFamilyCodeSnippetCommentEndLineTemplate, _context));
            AddExtractorItems(new[] { "haskell", "lua", "sql" },
                new CodeSnippetExtrator(SqlFamilyCodeSnippetCommentStartLineTemplate, SqlFamilyCodeSnippetCommentEndLineTemplate, _context));
            AddExtractorItems(new[] { "perl", "powershell", "python", "r", "ruby", "shell" },
                new CodeSnippetExtrator(ScriptFamilyCodeSnippetCommentStartLineTemplate, ScriptFamilyCodeSnippetCommentEndLineTemplate, _context));
            AddExtractorItems(new[] { "batchfile" },
                new CodeSnippetExtrator(BatchFileCodeSnippetRegionStartLineTemplate, BatchFileCodeSnippetRegionEndLineTemplate, _context));
            AddExtractorItems(new[] { "csharp", "cshtml" },
                new CodeSnippetExtrator(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, _context, false));
            AddExtractorItems(new[] { "erlang", "matlab" },
                new CodeSnippetExtrator(ErlangCodeSnippetRegionStartLineTemplate, ErlangCodeSnippetRegionEndLineTemplate, _context));
            AddExtractorItems(new[] { "lisp" },
                new CodeSnippetExtrator(LispCodeSnippetRegionStartLineTemplate, LispCodeSnippetRegionEndLineTemplate, _context));
            AddExtractorItems(new[] { "vb", "vbhtml" },
                new CodeSnippetExtrator(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, _context, false));
        }

        private void AddExtractorItems(string[] languages, CodeSnippetExtrator extractor)
        {
            foreach (var language in languages)
            {
                AddExtractorItem(language, extractor);

                if (LanguageAlias.ContainsKey(language))
                {
                    foreach (var alias in LanguageAlias[language])
                    {
                        AddExtractorItem(alias, extractor);
                    }
                }
            }
        }

        private void AddExtractorItem(string language, CodeSnippetExtrator extractor)
        {
            if (CodeLanguageExtractors.ContainsKey(language))
            {
                CodeLanguageExtractors[language].Add(extractor);
            }
            else
            {
                CodeLanguageExtractors[language] = new List<CodeSnippetExtrator> { extractor };
            }
        }

        protected override void Write(HtmlRenderer renderer, CodeSnippet codeSnippet)
        {
            var (content, codeSnippetPath) = _context.ReadFile(codeSnippet.CodePath, InclusionContext.File, codeSnippet);

            if (content == null)
            {
                _context.LogWarning("codesnippet-not-found", $"Cannot resolve '{codeSnippet.CodePath}' relative to '{InclusionContext.File}'.", codeSnippet);
                renderer.Write(GetWarning());
                return;
            }

            codeSnippet.SetAttributeString();

            renderer.Write("<pre><code").WriteAttributes(codeSnippet).Write(">");
            renderer.WriteEscape(GetContent(content, codeSnippet));
            renderer.Write("</code></pre>");
        }

        private string GetNoteBookContent(string content, string tagName, CodeSnippet obj)
        {
            JObject contentObject = null;
            try
            {
                contentObject = JObject.Parse(content);
            }
            catch (JsonReaderException ex)
            {
                _context.LogError("not-notebook-content", "Not a valid Notebook. " + ex.ToString(), obj);
                return string.Empty;
            }

            string sourceJsonPath = string.Format("$..cells[?(@.metadata.name=='{0}')].source", tagName);
            JToken sourceObject = null;
            try
            {
                sourceObject = contentObject.SelectToken(sourceJsonPath);
            }
            catch (JsonException)
            {
                _context.LogError("mutiple-tags-with-same-name", string.Format("Multiple entries with the name '{0}' where found in the notebook.", tagName), obj);
                return string.Empty;
            }

            if (sourceObject == null)
            {
                _context.LogError("tag-not-found", string.Format("The name '{0}' is not present in the notebook file.", tagName), obj);
                return string.Empty;
            }

            StringBuilder showCode = new StringBuilder();
            string[] lines = ((JArray)sourceObject).ToObject<string[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                showCode.Append(lines[i]);
            }

            return showCode.ToString();
        }

        private string GetContent(string content, CodeSnippet obj)
        {
            var allLines = ReadAllLines(content).ToArray();

            // code range priority: tag > #L1 > start/end > range > default
            if (!string.IsNullOrEmpty(obj.TagName))
            {
                var lang = obj.Language ?? Path.GetExtension(obj.CodePath);

                if (obj.IsNotebookCode)
                {
                    return GetNoteBookContent(content, obj.TagName, obj);
                }

                if (!CodeLanguageExtractors.TryGetValue(lang, out List<CodeSnippetExtrator> extrators))
                {
                    _context.LogError(
                        "unknown-language-code",
                        $"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead",
                        obj);
                }

                if (extrators != null)
                {
                    var tagWithPrefix = tagPrefix + obj.TagName;
                    foreach (var extrator in extrators)
                    {
                        HashSet<int> tagLines = new HashSet<int>();
                        var tagToCoderangeMapping = extrator.GetAllTags(allLines, ref tagLines);
                        CodeRange cr;
                        if (tagToCoderangeMapping.TryGetValue(obj.TagName, out cr)
                            || tagToCoderangeMapping.TryGetValue(tagWithPrefix, out cr))
                        {
                            return GetCodeLines(allLines, obj, new List<CodeRange> { cr }, tagLines);
                        }
                    }
                }
            }
            else if (obj.BookMarkRange != null)
            {
                return GetCodeLines(allLines, obj, new List<CodeRange> { obj.BookMarkRange });
            }
            else if (obj.StartEndRange != null)
            {
                return GetCodeLines(allLines, obj, new List<CodeRange> { obj.StartEndRange });
            }
            else if (obj.CodeRanges != null)
            {
                return GetCodeLines(allLines, obj, obj.CodeRanges);
            }
            else
            {
                return GetCodeLines(allLines, obj, new List<CodeRange> { new CodeRange() { Start = 0, End = allLines.Length } });
            }

            return string.Empty;
        }

        private static IEnumerable<string> ReadAllLines(string content)
        {
            string line;
            var reader = new StringReader(content);
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        private string GetCodeLines(string[] allLines, CodeSnippet obj, List<CodeRange> codeRanges, HashSet<int> ignoreLines = null)
        {
            List<string> codeLines = new List<string>();
            StringBuilder showCode = new StringBuilder();
            int commonIndent = int.MaxValue;

            foreach (var codeRange in codeRanges)
            {
                for (int lineNumber = Math.Max(codeRange.Start - 1, 0); lineNumber < Math.Min(codeRange.End, allLines.Length); lineNumber++)
                {
                    if (ignoreLines != null && ignoreLines.Contains(lineNumber)) continue;

                    if (IsBlankLine(allLines[lineNumber]))
                    {
                        codeLines.Add(allLines[lineNumber]);
                    }
                    else
                    {
                        int indentSpaces = 0;
                        string rawCodeLine = CountAndReplaceIndentSpaces(allLines[lineNumber], out indentSpaces);
                        commonIndent = Math.Min(commonIndent, indentSpaces);
                        codeLines.Add(rawCodeLine);
                    }
                }
            }

            int dedent = obj.DedentLength == null || obj.DedentLength < 0 ? commonIndent : (int)obj.DedentLength;

            foreach (var rawCodeLine in codeLines)
            {
                showCode.Append($"{DedentString(rawCodeLine, dedent)}\n");
            }

            return showCode.ToString();
        }

        private string DedentString(string source, int dedent)
        {
            int validDedent = Math.Min(dedent, source.Length);
            for (int i = 0; i < validDedent; i++)
            {
                if (source[i] != ' ') return source.Substring(i);
            }
            return source.Substring(validDedent);
        }

        private bool IsBlankLine(string line)
        {
            return line == "";
        }

        private string CountAndReplaceIndentSpaces(string line, out int count)
        {
            StringBuilder sb = new StringBuilder();
            count = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ')
                {
                    sb.Append(' ');
                    count++;
                }
                else if (c == '\t')
                {
                    int newCount = CharHelper.AddTab(count);
                    sb.Append(' ', newCount - count);
                    count = newCount;

                }
                else
                {
                    sb.Append(line, i, line.Length - i);
                    break;
                }
            }

            return sb.ToString();
        }

        private bool IsLineInRange(int lineNumber, List<CodeRange> allCodeRanges)
        {
            if (allCodeRanges.Count() == 0) return true;

            for (int rangeNumber = 0; rangeNumber < allCodeRanges.Count(); rangeNumber++)
            {
                var range = allCodeRanges[rangeNumber];
                if (lineNumber >= range.Start && lineNumber <= range.End)
                    return true;
            }

            return false;
        }

        private int GetTagLineNumber(string[] lines, string tagLine)
        {
            for (int index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var targetColumn = 0;
                var match = true;

                for (int column = 0; column < line.Length; column++)
                {
                    var c = line[column];
                    if (c != ' ')
                    {
                        if (targetColumn >= tagLine.Length || tagLine[targetColumn] != Char.ToUpper(c))
                        {
                            match = false;
                            break;
                        }

                        targetColumn++;
                    }
                }

                if (match && targetColumn == tagLine.Length) return index + 1;
            }

            return -1;
        }

        private string GetWarning()
        {
            var warningTitle = _context.GetToken(warningTitleId) ?? defaultWarningTitle;
            var warningMessage = _context.GetToken(warningMessageId) ?? defaultWarningMessage;

            return string.Format(@"<div class=""WARNING"">
{0}
<p>{1}</p>
</div>", warningTitle, warningMessage);

        }
    }
}