// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    using Markdig.Helpers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class HtmlCodeSnippetRenderer : HtmlObjectRenderer<CodeSnippet>
    {
        private MarkdownContext _context;
        private IMarkdownEngine _engine;
        private const string tagPrefix = "snippet";

        private static readonly IReadOnlyDictionary<string, List<string>> LanguageAlias = new Dictionary<string, List<string>>
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

        public HtmlCodeSnippetRenderer(IMarkdownEngine engine, MarkdownContext context)
        {
            _engine = engine;
            _context = context;

            BuildCodeLanguageExtractors();
        }

        private void BuildCodeLanguageExtractors()
        {
            AddExtractorItems(new[] { "vb", "vbhtml" }, 
                new CodeSnippetExtrator(BasicFamilyCodeSnippetCommentStartLineTemplate, BasicFamilyCodeSnippetCommentEndLineTemplate));
            AddExtractorItems(new[] { "actionscript", "arduino", "assembly", "cpp", "csharp", "cshtml", "cuda", "d", "fsharp", "go", "java", "javascript", "pascal", "php", "processing", "rust", "scala", "smalltalk", "swift", "typescript" },
                new CodeSnippetExtrator(CFamilyCodeSnippetCommentStartLineTemplate, CFamilyCodeSnippetCommentEndLineTemplate));
            AddExtractorItems(new[] { "xml", "xaml", "html", "cshtml", "vbhtml" },
                new CodeSnippetExtrator(MarkupLanguageFamilyCodeSnippetCommentStartLineTemplate, MarkupLanguageFamilyCodeSnippetCommentEndLineTemplate));
            AddExtractorItems(new[] { "haskell", "lua", "sql" },
                new CodeSnippetExtrator(SqlFamilyCodeSnippetCommentStartLineTemplate, SqlFamilyCodeSnippetCommentEndLineTemplate));
            AddExtractorItems(new[] { "perl", "powershell", "python", "r", "ruby", "shell" },
                new CodeSnippetExtrator(ScriptFamilyCodeSnippetCommentStartLineTemplate, ScriptFamilyCodeSnippetCommentEndLineTemplate));
            AddExtractorItems(new[] { "batchfile" },
                new CodeSnippetExtrator(BatchFileCodeSnippetRegionStartLineTemplate, BatchFileCodeSnippetRegionEndLineTemplate));
            AddExtractorItems(new[] { "csharp", "cshtml" },
                new CodeSnippetExtrator(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, false));
            AddExtractorItems(new[] { "erlang", "matlab" },
                new CodeSnippetExtrator(ErlangCodeSnippetRegionStartLineTemplate, ErlangCodeSnippetRegionEndLineTemplate));
            AddExtractorItems(new[] { "lisp" },
                new CodeSnippetExtrator(LispCodeSnippetRegionStartLineTemplate, LispCodeSnippetRegionEndLineTemplate));
            AddExtractorItems(new[] { "vb", "vbhtml" },
                new CodeSnippetExtrator(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, false));
        }

        private void AddExtractorItems(string[] languages, CodeSnippetExtrator extractor)
        {
            foreach (var language in languages)
            {
                AddExtractorItem(language, extractor);
                
                if(LanguageAlias.ContainsKey(language))
                {
                    foreach(var alias in LanguageAlias[language])
                    {
                        AddExtractorItem(alias, extractor);
                    }
                }
            }
        }

        private void AddExtractorItem(string language, CodeSnippetExtrator extractor)
        {
            if(CodeLanguageExtractors.ContainsKey(language))
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
            var refFileRelativePath = ((RelativePath)codeSnippet.CodePath).BasedOn((RelativePath)_context.FilePath);

            if (!EnvironmentContext.FileAbstractLayer.Exists(refFileRelativePath))
            {
                string tag = "ERROR CODESNIPPET";
                string message = $"Unable to find {refFileRelativePath}";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, codeSnippet.Raw, codeSnippet.Line);
                return;
            }

            if (codeSnippet.DedentLength != null && codeSnippet.DedentLength < 0)
            {
                renderer.Write($"<!-- Dedent length {codeSnippet.DedentLength} should be positive. Auto-dedent will be applied. -->\n");
            }

            codeSnippet.SetAttributeString();

            renderer.Write("<pre><code").WriteAttributes(codeSnippet).Write(">");
            renderer.WriteEscape(GetContent(codeSnippet));
            renderer.Write("</code></pre>");
        }

        private string GetContent(CodeSnippet obj)
        {
            var currentFilePath = ((RelativePath)_context.FilePath).GetPathFromWorkingFolder();
            var refFileRelativePath = ((RelativePath)obj.CodePath).BasedOn(currentFilePath);
            _engine.ReportDependency(refFileRelativePath);

            var refPath = Path.Combine(_context.BasePath, refFileRelativePath.RemoveWorkingFolder());
            var allLines = EnvironmentContext.FileAbstractLayer.ReadAllLines(refFileRelativePath);

            // code range priority: tag > #L1 > start/end > range > default
            if (!string.IsNullOrEmpty(obj.TagName))
            {
                var lang = obj.Language ?? Path.GetExtension(refPath);
                if (!CodeLanguageExtractors.TryGetValue(lang, out List<CodeSnippetExtrator> extrators))
                {
                    Logger.LogError($"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead");
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
    }
}
