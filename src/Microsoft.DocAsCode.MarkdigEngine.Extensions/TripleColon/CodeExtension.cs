// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class CodeExtension : ITripleColonExtensionInfo
    {
        public string Name => "code";
        public bool SelfClosing => true;
        public bool EndingTripleColons => false;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }

        private readonly MarkdownContext _context;
        private static Regex tagRegex = new Regex(@"(?:<!--|--|//|'|rem|%|;|#)\s*<\s*.*\s*?>|#region|#endregion");

        public CodeExtension(MarkdownContext context)
        {
            _context = context;
        }

        public bool Render(HtmlRenderer renderer, TripleColonBlock block)
        {
            return RenderDelegate != null
                ? RenderDelegate(renderer, block)
                : false;
        }

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, TripleColonBlock block)
        {
            htmlAttributes = null;
            renderProperties = new Dictionary<string, string>();
            var source = string.Empty;
            var range = string.Empty;
            var id = string.Empty;
            var highlight = string.Empty;
            var language = string.Empty;
            var interactive = string.Empty;

            foreach (var attribute in attributes)
            {
                var name = attribute.Key;
                var value = attribute.Value;
                switch (name)
                {
                    case "source":
                        source = value;
                        break;
                    case "range":
                        range = value;
                        break;
                    case "id":
                        id = value;
                        break;
                    case "highlight":
                        highlight = value;
                        break;
                    case "language":
                        language = value;
                        break;
                    case "interactive":
                        interactive = value;
                        break;
                    default:
                        logError($"Unexpected attribute \"{name}\".");
                        return false;
                }
            }

            if (string.IsNullOrEmpty(source))
            {
                logError("source is a required attribute. Please ensure you have specified a source attribute");
                return false;
            }

            if(string.IsNullOrEmpty(language))
            {
                language = InferLanguageFromFile(source, logError);
            }

            htmlAttributes = new HtmlAttributes();
            htmlAttributes.AddProperty("class", $"lang-{language}");
            if (!string.IsNullOrEmpty(interactive))
            {
                htmlAttributes.AddProperty("data-interactive", language);
                htmlAttributes.AddProperty("data-interactive-mode", interactive);
            }
            if (!string.IsNullOrEmpty(highlight)) htmlAttributes.AddProperty("highlight-lines", highlight);

            RenderDelegate = (renderer, obj) =>
            {
                var currentId = string.Empty;
                var currentRange = string.Empty;
                var currentSource = string.Empty;
                obj.Attributes.TryGetValue("id", out currentId); //it's okay if this is null
                obj.Attributes.TryGetValue("range", out currentRange); //it's okay if this is null
                obj.Attributes.TryGetValue("source", out currentSource); //source has already been checked above
                var (code, codePath) = _context.ReadFile(currentSource, obj);
                if (string.IsNullOrEmpty(code))
                {
                    logWarning($"The code snippet \"{currentSource}\" could not be found.");
                    return false;
                }
                //var updatedCode = GetCodeSnippet(currentRange, currentId, code, logError).TrimEnd();
                var htmlCodeSnippetRenderer = new HtmlCodeSnippetRenderer(_context);
                var snippet = new CodeSnippet(null);
                snippet.CodePath = source;
                snippet.TagName = currentId;
                List<CodeRange> ranges;
                TryGetLineRanges(currentRange, out ranges);
                snippet.CodeRanges = ranges;
                var updatedCode = htmlCodeSnippetRenderer.GetContent(code, snippet);
                updatedCode = ExtensionsHelper.Escape(updatedCode).TrimEnd();

                if (updatedCode == string.Empty)
                {
                    return false;
                }
                renderer.WriteLine("<pre>");
                renderer.Write("<code").WriteAttributes(obj).Write(">");
                renderer.WriteLine(updatedCode);
                renderer.WriteLine("</code></pre>");

                return true;
            };

            return true;
        }

        private bool TryGetLineRanges(string query, out List<CodeRange> codeRanges)
        {
            codeRanges = null;
            if (string.IsNullOrEmpty(query)) return false;

            var rangesSplit = query.Split(new[] { ',' });

            foreach (var range in rangesSplit)
            {
                if (!TryGetLineRange(range, out var codeRange, false))
                {
                    return false;
                }

                if (codeRanges == null)
                {
                    codeRanges = new List<CodeRange>();
                }

                codeRanges.Add(codeRange);
            }

            return true;
        }

        private bool TryGetLineRange(string query, out CodeRange codeRange, bool withL = true)
        {
            codeRange = null;
            if (string.IsNullOrEmpty(query)) return false;

            int endLine;

            var splitLine = query.Split(new[] { '-' });
            if (splitLine.Length > 2) return false;

            var result = TryGetLineNumber(splitLine[0], out var startLine, withL);
            endLine = startLine;

            if (splitLine.Length > 1)
            {
                result &= TryGetLineNumber(splitLine[1], out endLine, withL);
            }

            codeRange = new CodeRange { Start = startLine, End = endLine };

            return result;
        }

        private bool TryGetLineNumber(string lineNumberString, out int lineNumber, bool withL = true)
        {
            lineNumber = int.MaxValue;
            if (string.IsNullOrEmpty(lineNumberString)) return true;

            if (withL && (lineNumberString.Length < 2 || Char.ToUpper(lineNumberString[0]) != 'L')) return false;

            return int.TryParse(withL ? lineNumberString.Substring(1) : lineNumberString, out lineNumber);

        }

        private string InferLanguageFromFile(string source, Action<string> logError)
        {
            var fileExtension = Path.GetExtension(source);
            if(fileExtension == null)
            {
                logError("Language is not set, and your source has no file type. Cannot infer language.");
            }
            var language = HtmlCodeSnippetRenderer.LanguageAlias.Where(oo => oo.Value.Contains(fileExtension) || oo.Value.Contains($".{fileExtension}")).FirstOrDefault();
            if(string.IsNullOrEmpty(language.Key))
            {
                logError("Language is not set, and we could not infer language from the file type.");
            }
            return language.Key;
        }

        private static string GetCodeSnippet(string range, string id, string code, Action<string> logError)
        {
            if(!string.IsNullOrEmpty(range) && !string.IsNullOrEmpty(id))
            {
                logError("You must set only either Range or Id, but not both.");
            }
            
            var codeSections = new List<string>();

            if (!string.IsNullOrEmpty(range))
            {
                var codeLines = code.Split('\n');
                codeSections = GetCodeSectionsFromRange(range, codeLines, codeSections, logError);
            }
            else if (!string.IsNullOrEmpty(id))
            {
                var codeLines = code.Split('\n');
                var beg = codeLines.FindIndexOfTag(id);
                var end = codeLines.FindIndexOfTag(id, true);
                if(end == 0)
                {
                    logError($"Could not find snippet id '{id}'. Make sure your snippet is in your source file.");
                }
                codeSections = GetCodeSectionsFromRange($"{beg}-{end}", codeLines, codeSections, logError, false);
            }
            else
            {
                codeSections.Add(code);
            }

            if (codeSections == null)
            {
                return string.Empty;
            }

            codeSections = Dedent(codeSections);
            var source = string.Join("\n", codeSections.ToArray());
            source = ExtensionsHelper.Escape(source);
            return source;
        }

        public static List<string> Dedent(List<string> sections)
        {
            var indentRegex = new Regex(@"^\s*\W");
            var RemoveIndentSpacesRegexString = @"^[ \t]{{1,{0}}}";
            var dedentedSections = new List<string>();
            foreach (var section in sections)
            {
                var codeLines = section.Split('\n');
                var length = 0;

                if (codeLines.Any(oo => !string.IsNullOrWhiteSpace(oo)))
                {
                    length = codeLines.Where(oo => !string.IsNullOrWhiteSpace(oo))
                                          .Min(oo =>
                    {
                        var matches = indentRegex.Matches(oo);
                        if (matches.Count > 0)
                        {
                            var match = matches[0];
                            return match.Value.Length;
                        } else
                        {
                            return 0;
                        }
                    });
                }

                var normalizedLines = (length == 0 ? codeLines : codeLines.Select(s => Regex.Replace(s, string.Format(RemoveIndentSpacesRegexString, length), string.Empty))).ToArray();
                dedentedSections.Add(string.Join("\n", normalizedLines));
            }
            return dedentedSections;
        }

        private static List<string> GetCodeSectionsFromRange(string range, string[] codeLines, List<string> codeSections, Action<string> logError, bool shouldKeepSnippetTags = true)
        {
            var ranges = range.Split(',');
            foreach (var codeRange in ranges)
            {
                if (codeRange.Contains("-"))
                {
                    var rangeParts = codeRange.Split('-');
                    if (!string.IsNullOrEmpty(rangeParts[1]))
                    {
                        var beg = 0;
                        var end = 0;
                        if(int.TryParse(rangeParts[0], out beg)
                            && int.TryParse(rangeParts[1], out end))
                        {
                            beg--;
                            end--;
                        } else
                        {
                            logError("Your ranges must be numbers.");
                            return null;
                        }
                        if(beg > codeLines.Length || end > codeLines.Length)
                        {
                            logError("Your range is greater than the number of lines in the document.");
                            return null;
                        }
                        var section = string.Empty;
                        for (var i = beg; i <= end; i++)
                        {
                            if(shouldKeepSnippetTags || !tagRegex.IsMatch(codeLines[i]))
                            {
                                section += codeLines[i] + "\n";
                            }
                        }
                        codeSections.Add(section);
                    }
                    else
                    {
                        var section = string.Empty;
                        var beg = 0;
                        if (int.TryParse(rangeParts[0], out beg))
                        {
                            beg--;
                        }
                        else
                        {
                            logError("Your ranges must be numbers.");
                            return null;
                        }
                        var end = codeLines.Length;
                        for (var i = beg; i < end; i++)
                        {
                            if (shouldKeepSnippetTags || !tagRegex.IsMatch(codeLines[i]))
                            {
                                section += codeLines[i] + "\n";
                            }
                        }
                        codeSections.Add(section);
                    }
                }
                else
                {
                    var beg = 0;
                    if (int.TryParse(codeRange, out beg))
                    {
                        codeSections.Add(codeLines[beg-1]);
                    }
                    else
                    {
                        logError("Your ranges must be numbers.");
                        return null;
                    }
                }
            }

            return codeSections;
        }

        public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
        {
            return true;
        }
    }

    public static class CodeTagExtensions
    {
        public static int FindIndexOfTag(this string[] codeLines, string id, bool isEnd = false)
        {
            if (!isEnd)
            {
                return Array.FindIndex(codeLines, line => line.IndexOf(id, StringComparison.OrdinalIgnoreCase) > -1) + 2;
            } else
            {
                Regex endTagRegex = new Regex($"\\s*(?:</\\s*{id}\\s*?>)|(?:(?:;|%)\\s*<\\s*{id}\\s*?>)", RegexOptions.IgnoreCase);
                var startTagIndex = Array.FindIndex(codeLines, line => line.IndexOf(id, StringComparison.OrdinalIgnoreCase) > -1) + 2;
                var endTagIndex = Array.FindIndex(codeLines, startTagIndex, line => {
                    var match = endTagRegex.Match(line);
                    return match.Success;
                });

                if(endTagIndex == -1) //search for region then
                {
                    var endRegionIndex1 = Array.FindIndex(codeLines, startTagIndex, line => line.IndexOf("endRegion", StringComparison.OrdinalIgnoreCase) > -1) + 1;
                    var endRegionIndex2 = Array.FindIndex(codeLines, endRegionIndex1, line => line.IndexOf("endRegion", StringComparison.OrdinalIgnoreCase) > -1) + 1;
                    var region2Index = Array.FindIndex(codeLines, startTagIndex, line => line.IndexOf("#region", StringComparison.OrdinalIgnoreCase) > -1);

                    if(endRegionIndex2 == -1)
                    {
                        return endRegionIndex1;
                    } else
                    {
                        if(region2Index > -1 && endRegionIndex1 > region2Index)
                        {
                            return endRegionIndex2;
                        } else
                        {
                            return endRegionIndex1;
                        }
                    }
                } else
                {
                    return endTagIndex;
                }
            }
        }
    }
}
