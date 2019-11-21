// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Parsers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public class CodeExtension : ITripleColonExtensionInfo
    {
        public string Name => "code";
        public bool SelfClosing => true;
        public bool EndingTripleColons => true;
        public Func<HtmlRenderer, TripleColonBlock, bool> RenderDelegate { get; private set; }

        private readonly MarkdownContext _context;

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

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, BlockProcessor processor)
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
                logError($"source is a required attribute. Please ensure you have specified a source attribute");
                return false;
            }
            var (code, codePath) = _context.ReadFile(source, InclusionContext.File, null);
            if(string.IsNullOrEmpty(code))
            {
                logError($"The code snippet \"{source}\" could not be found.");
                return false;
            }
            
            var (updatedCode, updatedHighlight) = GetCodeSnippet(range, id, code, highlight, logError);

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
            htmlAttributes.AddProperty("name", "main");
            htmlAttributes.AddProperty("title", "__________");
            if (!string.IsNullOrEmpty(highlight)) htmlAttributes.AddProperty("highlight-lines", highlight);

            RenderDelegate = (renderer, obj) =>
            {
                renderer.WriteLine("<pre>");
                renderer.Write("<code").WriteAttributes(obj).WriteLine(">");
                renderer.WriteLine(updatedCode);
                renderer.WriteLine("</code></pre>");

                return true;
            };

            return true;
        }

        private string InferLanguageFromFile(string source, Action<string> logError)
        {
            var fileExtension = source.Split('.').ToList().LastOrDefault();
            if(fileExtension == null)
            {
                logError($"Language is not set, and your source has no file type. Cannot infer language.");
            }
            var language = HtmlCodeSnippetRenderer.LanguageAlias.Where(oo => oo.Value.Contains(fileExtension)).FirstOrDefault();
            return language.Key;
        }

        private static Tuple<string, string> GetCodeSnippet(string range, string id, string code, string highlight, Action<string> logError)
        {
            var highlightOffset = 0;

            if(!string.IsNullOrEmpty(range) && !string.IsNullOrEmpty(id))
            {
                logError($"You must set only either Range or Id, but not both.");
            }
            
            var codeSections = new List<string>();

            if (!string.IsNullOrEmpty(range))
            {
                codeSections = GetCodeSectionsFromRange(range, code, codeSections);
            }
            else if (!string.IsNullOrEmpty(id))
            {
                var strRegexStart = @"((<|</|#region)\s*" + id + @"(>|\s*|\n*))";
                var idRegexStart = new Regex(strRegexStart);
                var idRegexEnd = new Regex(strRegexStart + @"|#endregion");
                var codeLines = code.Split('\n').ToList();
                var beg = codeLines.FindIndex(oo => idRegexStart.IsMatch(oo)) + 1;
                var end = codeLines.FindLastIndex(oo => idRegexEnd.IsMatch(oo)) - 1;
                codeSections = GetCodeSectionsFromRange($"{beg}-{end}", code, codeSections);
            }
            else
            {
                codeSections.Add(code);
            }

            codeSections = Dedent(codeSections);
            var source = string.Join("    ...\n", codeSections.ToArray());
            return new Tuple<string, string>(source, "3-4"); ;
        }

        public static List<string> Dedent(List<string> sections)
        {
            var indentRegex = new Regex(@"^\s*\W");
            var RemoveIndentSpacesRegexString = @"^[ \t]{{1,{0}}}";
            var dedentedSections = new List<string>();
            foreach (var section in sections)
            {
                var codeLines = section.Split('\n');
                var length = codeLines.Where(oo => !string.IsNullOrWhiteSpace(oo))
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
                var normalizedLines = (length == 0 ? codeLines : codeLines.Select(s => Regex.Replace(s, string.Format(RemoveIndentSpacesRegexString, length), string.Empty))).ToArray();
                dedentedSections.Add(string.Join("\n", normalizedLines));
            }
            //return normalizedLines;
            return dedentedSections;
        }

        private static List<string> GetCodeSectionsFromRange(string range, string code, List<string> codeSections)
        {
            var highlightOffset = 0;
            var codeLines = code.Split('\n');

            range = range.Replace(" ", "");
            var ranges = range.Split(',');
            foreach (var codeRange in ranges)
            {
                if (codeRange.Contains("-"))
                {
                    var rangeParts = codeRange.Split('-');
                    if (!string.IsNullOrEmpty(rangeParts[1]))
                    {
                        var beg = Convert.ToInt16(rangeParts[0]);
                        var end = Convert.ToInt16(rangeParts[1]);
                        var section = string.Empty;
                        for (var i = beg; i <= end; i++)
                        {
                            section += codeLines[i] + "\n";
                        }
                        codeSections.Add(section);
                    }
                    else
                    {
                        var section = string.Empty;
                        var beg = Convert.ToInt16(rangeParts[0]);
                        var end = codeLines.Length;
                        for (var i = beg; i < end; i++)
                        {
                            section += codeLines[i] + "\n";
                        }
                        codeSections.Add(section);
                    }
                }
                else
                {
                    codeSections.Add(codeLines[Convert.ToInt16(codeRange)]);
                }
            }

            return codeSections;
        }

        public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
        {
            return true;
        }
    }
}
