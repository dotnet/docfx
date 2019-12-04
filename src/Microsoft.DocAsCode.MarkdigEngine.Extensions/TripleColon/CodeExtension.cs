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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public class CodeExtension : ITripleColonExtensionInfo
    {
        public string Name => "code";
        public bool SelfClosing => true;
        public bool EndingTripleColons => false;
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
                var (code, codePath) = _context.ReadFile(currentSource, InclusionContext.File, obj);
                if (string.IsNullOrEmpty(code))
                {
                    logError($"The code snippet \"{source}\" could not be found.");
                    return false;
                }
                var updatedCode = GetCodeSnippet(currentRange, currentId, code, logError).TrimEnd();

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

        private string InferLanguageFromFile(string source, Action<string> logError)
        {
            var fileExtension = Path.GetExtension(source);
            if(fileExtension == null)
            {
                logError($"Language is not set, and your source has no file type. Cannot infer language.");
            }
            var language = HtmlCodeSnippetRenderer.LanguageAlias.Where(oo => oo.Value.Contains(fileExtension) || oo.Value.Contains($".{fileExtension}")).FirstOrDefault();
            if(string.IsNullOrEmpty(language.Key))
            {
                logError($"Language is not set, and we could not infer language from the file type.");
            }
            return language.Key;
        }

        private static string GetCodeSnippet(string range, string id, string code, Action<string> logError)
        {
            if(!string.IsNullOrEmpty(range) && !string.IsNullOrEmpty(id))
            {
                logError($"You must set only either Range or Id, but not both.");
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
                var beg = Array.FindIndex(codeLines, line => line.Replace(" ", "").IndexOf($"<{id}>", StringComparison.OrdinalIgnoreCase) > -1) + 2;
                var end = Array.FindIndex(codeLines, line => line.Replace(" ", "").IndexOf($"</{id}>", StringComparison.OrdinalIgnoreCase) > -1);
                codeSections = GetCodeSectionsFromRange($"{beg}-{end}", codeLines, codeSections, logError);
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
            var source = string.Join("    ...\n", codeSections.ToArray());
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
            return dedentedSections;
        }

        private static List<string> GetCodeSectionsFromRange(string range, string[] codeLines, List<string> codeSections, Action<string> logError)
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
                            logError($"Your ranges must be numbers.");
                            return null;
                        }
                        if(beg > codeLines.Length || end > codeLines.Length)
                        {
                            logError($"Your range is greater than the number of lines in the document.");
                            return null;
                        }
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
                        var beg = 0;
                        if (int.TryParse(rangeParts[0], out beg))
                        {
                            beg--;
                        }
                        else
                        {
                            logError($"Your ranges must be numbers.");
                            return null;
                        }
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
                    var beg = 0;
                    if (int.TryParse(codeRange, out beg))
                    {
                        codeSections.Add(codeLines[beg--]);
                    }
                    else
                    {
                        logError($"Your ranges must be numbers.");
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
}
