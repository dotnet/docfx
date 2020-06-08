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
        public Func<HtmlRenderer, MarkdownObject, bool> RenderDelegate { get; private set; }

        private readonly MarkdownContext _context;
        private static Regex tagRegex = new Regex(@"(?:<!--|--|//|'|rem|%|;|#)\s*<\s*.*\s*?>|#region|#endregion");

        public CodeExtension(MarkdownContext context)
        {
            _context = context;
        }

        public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject)
        {
            var block = (TripleColonBlock)markdownObject;
            return RenderDelegate != null
                ? RenderDelegate(renderer, block)
                : false;
        }

        public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
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
                var block = (TripleColonBlock)obj;
                var currentId = string.Empty;
                var currentRange = string.Empty;
                var currentSource = string.Empty;
                block.Attributes.TryGetValue("id", out currentId); //it's okay if this is null
                block.Attributes.TryGetValue("range", out currentRange); //it's okay if this is null
                block.Attributes.TryGetValue("source", out currentSource); //source has already been checked above
                var (code, codePath) = _context.ReadFile(currentSource, obj);
                if (string.IsNullOrEmpty(code))
                {
                    logWarning($"The code snippet \"{currentSource}\" could not be found.");
                    return false;
                }
                //var updatedCode = GetCodeSnippet(currentRange, currentId, code, logError).TrimEnd();
                var htmlCodeSnippetRenderer = new HtmlCodeSnippetRenderer(_context);
                var snippet = new CodeSnippet(null);
                snippet.CodePath = currentSource;
                snippet.TagName = currentId;
                List<CodeRange> ranges;
                HtmlCodeSnippetRenderer.TryGetLineRanges(currentRange, out ranges);
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

        public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
        {
            return true;
        }
    }
}
