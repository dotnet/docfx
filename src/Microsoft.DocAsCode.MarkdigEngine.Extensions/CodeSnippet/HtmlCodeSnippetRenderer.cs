// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
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

    public class HtmlCodeSnippetRenderer : HtmlObjectRenderer<CodeSnippet>
    {
        private MarkdownContext _context;
        private IMarkdownEngine _engine;
        private const string tagPrefix = "snippet";

        // C# code snippet comment block: // <[/]snippetname>
        private static readonly string CSharpCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string CSharpCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

        // C# code snippet region block: start -> #region snippetname, end -> #endregion
        private static readonly string CSharpCodeSnippetRegionStartLineTemplate = "#region{tagname}";
        private static readonly string CSharpCodeSnippetRegionEndLineTemplate = "#endregion";

        // VB code snippet comment block: ' <[/]snippetname>
        private static readonly string VBCodeSnippetCommentStartLineTemplate = "'<{tagname}>";
        private static readonly string VBCodeSnippetCommentEndLineTemplate = "'</{tagname}>";

        // VB code snippet Region block: start -> # Region "snippetname", end -> # End Region
        private static readonly string VBCodeSnippetRegionRegionStartLineTemplate = "#region{tagname}";
        private static readonly string VBCodeSnippetRegionRegionEndLineTemplate = "#endregion";

        // C++ code snippet block: // <[/]snippetname>
        private static readonly string CPlusPlusCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string CPlusPlusCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

        // F# code snippet block: // <[/]snippetname>
        private static readonly string FSharpCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string FSharpCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

        // XML code snippet block: <!-- <[/]snippetname> -->
        private static readonly string XmlCodeSnippetCommentStartLineTemplate = "<!--<{tagname}>-->";
        private static readonly string XmlCodeSnippetCommentEndLineTemplate = "<!--</{tagname}>-->";

        // XAML code snippet block: <!-- <[/]snippetname> -->
        private static readonly string XamlCodeSnippetCommentStartLineTemplate = "<!--<{tagname}>-->";
        private static readonly string XamlCodeSnippetCommentEndLineTemplate = "<!--</{tagname}>-->";

        // HTML code snippet block: <!-- <[/]snippetname> -->
        private static readonly string HtmlCodeSnippetCommentStartLineTemplate = "<!--<{tagname}>-->";
        private static readonly string HtmlCodeSnippetCommentEndLineTemplate = "<!--</{tagname}>-->";

        // Sql code snippet block: -- <[/]snippetname>
        private static readonly string SqlCodeSnippetCommentStartLineTemplate = "--<{tagname}>";
        private static readonly string SqlCodeSnippetCommentEndLineTemplate = "--</{tagname}>";

        // Javascript code snippet block: <!-- <[/]snippetname> -->
        private static readonly string JavaScriptSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string JavaScriptSnippetCommentEndLineTemplate = "//</{tagname}>";

        // Java code snippet comment block: // <[/]snippetname>
        private static readonly string JavaCodeSnippetCommentStartLineTemplate = "//<{tagname}>";
        private static readonly string JavaCodeSnippetCommentEndLineTemplate = "//</{tagname}>";

        // Python code snippet comment block: # <[/]snippetname>
        private static readonly string PythonCodeSnippetCommentStartLineTemplate = "#<{tagname}>";
        private static readonly string PythonCodeSnippetCommentEndLineTemplate = "#</{tagname}>";

        // Language names and aliases fllow http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases
        // Language file extensions follow https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
        // Currently only supports parts of the language names, aliases and extensions
        // Later we can move the repository's supported/custom language names, aliases, extensions and corresponding comments regexes to docfx build configuration
        private static readonly IReadOnlyDictionary<string, List<CodeSnippetExtrator>> CodeLanguageExtractors =
            new Dictionary<string, List<CodeSnippetExtrator>>(StringComparer.OrdinalIgnoreCase)
            {
                [".cs"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CSharpCodeSnippetCommentStartLineTemplate, CSharpCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, false)
                },
                ["cs"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CSharpCodeSnippetCommentStartLineTemplate, CSharpCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, false)
                },
                ["csharp"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CSharpCodeSnippetCommentStartLineTemplate, CSharpCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(CSharpCodeSnippetRegionStartLineTemplate, CSharpCodeSnippetRegionEndLineTemplate, false)
                },
                [".vb"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(VBCodeSnippetCommentStartLineTemplate, VBCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, false)
                },
                ["vb"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(VBCodeSnippetCommentStartLineTemplate, VBCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, false)
                },
                ["vbnet"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(VBCodeSnippetCommentStartLineTemplate, VBCodeSnippetCommentEndLineTemplate),
                    new CodeSnippetExtrator(VBCodeSnippetRegionRegionStartLineTemplate, VBCodeSnippetRegionRegionEndLineTemplate, false)
                },
                [".cpp"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                [".h"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                [".hpp"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                [".c"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                [".cc"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                ["cpp"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                ["c++"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(CPlusPlusCodeSnippetCommentStartLineTemplate, CPlusPlusCodeSnippetCommentEndLineTemplate)
                },
                ["fs"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(FSharpCodeSnippetCommentStartLineTemplate, FSharpCodeSnippetCommentEndLineTemplate)
                },
                ["fsharp"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(FSharpCodeSnippetCommentStartLineTemplate, FSharpCodeSnippetCommentEndLineTemplate)
                },
                [".fs"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(FSharpCodeSnippetCommentStartLineTemplate, FSharpCodeSnippetCommentEndLineTemplate)
                },
                [".fsi"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(FSharpCodeSnippetCommentStartLineTemplate, FSharpCodeSnippetCommentEndLineTemplate)
                },
                [".fsx"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(FSharpCodeSnippetCommentStartLineTemplate, FSharpCodeSnippetCommentEndLineTemplate)
                },
                [".xml"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XmlCodeSnippetCommentStartLineTemplate, XmlCodeSnippetCommentEndLineTemplate)
                },
                [".csdl"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XmlCodeSnippetCommentStartLineTemplate, XmlCodeSnippetCommentEndLineTemplate)
                },
                [".edmx"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XmlCodeSnippetCommentStartLineTemplate, XmlCodeSnippetCommentEndLineTemplate)
                },
                ["xml"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XmlCodeSnippetCommentStartLineTemplate, XmlCodeSnippetCommentEndLineTemplate)
                },
                [".html"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(HtmlCodeSnippetCommentStartLineTemplate, HtmlCodeSnippetCommentEndLineTemplate)
                },
                ["html"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(HtmlCodeSnippetCommentStartLineTemplate, HtmlCodeSnippetCommentEndLineTemplate)
                },
                [".xaml"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XamlCodeSnippetCommentStartLineTemplate, XamlCodeSnippetCommentEndLineTemplate)
                },
                ["xaml"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(XamlCodeSnippetCommentStartLineTemplate, XamlCodeSnippetCommentEndLineTemplate)
                },
                [".sql"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(SqlCodeSnippetCommentStartLineTemplate, SqlCodeSnippetCommentEndLineTemplate)
                },
                ["sql"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(SqlCodeSnippetCommentStartLineTemplate, SqlCodeSnippetCommentEndLineTemplate)
                },
                [".js"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(JavaScriptSnippetCommentStartLineTemplate, JavaScriptSnippetCommentEndLineTemplate)
                },
                ["js"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(JavaScriptSnippetCommentStartLineTemplate, JavaScriptSnippetCommentEndLineTemplate)
                },
                ["javascript"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(JavaScriptSnippetCommentStartLineTemplate, JavaScriptSnippetCommentEndLineTemplate)
                },
                [".java"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(JavaCodeSnippetCommentStartLineTemplate, JavaCodeSnippetCommentEndLineTemplate)
                },
                ["java"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(JavaCodeSnippetCommentStartLineTemplate, JavaCodeSnippetCommentEndLineTemplate)
                },
                [".py"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(PythonCodeSnippetCommentStartLineTemplate, PythonCodeSnippetCommentEndLineTemplate)
                },
                ["python"] = new List<CodeSnippetExtrator>
                {
                    new CodeSnippetExtrator(PythonCodeSnippetCommentStartLineTemplate, PythonCodeSnippetCommentEndLineTemplate)
                }
            };

        public HtmlCodeSnippetRenderer(IMarkdownEngine engine, MarkdownContext context)
        {
            _engine = engine;
            _context = context;
        }

        protected override void Write(HtmlRenderer renderer, CodeSnippet obj)
        {
            var refFileRelativePath = ((RelativePath)obj.CodePath).BasedOn((RelativePath)_context.FilePath);
            var refPath = Path.Combine(_context.BasePath, refFileRelativePath.RemoveWorkingFolder());
            if (!File.Exists(refPath))
            {
                string tag = "ERROR CODESNIPPET";
                string message = $"Unable to find {refFileRelativePath}";
                ExtensionsHelper.GenerateNodeWithCommentWrapper(renderer, tag, message, obj.Raw, obj.Line);
                return;
            }
            
            if(obj.DedentLength != null && obj.DedentLength < 0)
            {
                renderer.Write($"<!-- Dedent length {obj.DedentLength} should be positive. Auto-dedent will be applied. -->\n");
            }

            renderer.Write("<pre><code").WriteAttributes(obj).Write(">");
            renderer.WriteEscape(GetContent(obj));
            renderer.Write("</code></pre>");
        }

        private string GetContent(CodeSnippet obj)
        {
            var currentFilePath = ((RelativePath)_context.FilePath).GetPathFromWorkingFolder();
            var refFileRelativePath = ((RelativePath)obj.CodePath).BasedOn(currentFilePath);
            _engine.ReportDependency(refFileRelativePath);
            
            var refPath = Path.Combine(_context.BasePath, refFileRelativePath.RemoveWorkingFolder());
            var allLines = File.ReadAllLines(refPath);

            // code range priority: tag > #L1 > start/end > range > default
            if (!string.IsNullOrEmpty(obj.TagName))
            {
                var lang = obj.Language ?? Path.GetExtension(refPath);
                List<CodeSnippetExtrator> extrators;
                if(!CodeLanguageExtractors.TryGetValue(lang, out extrators))
                {
                    Logger.LogError($"{lang} is not supported languaging name, alias or extension for parsing code snippet with tag name, you can use line numbers instead");
                }

                if(extrators != null)
                {
                    var tagWithPrefix = tagPrefix + obj.TagName;
                    foreach(var extrator in extrators)
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
