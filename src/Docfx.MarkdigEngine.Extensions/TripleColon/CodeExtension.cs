// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class CodeExtension : ITripleColonExtensionInfo
{
    public string Name => "code";

    public bool SelfClosing => true;

    public static bool EndingTripleColons => false;

    public bool IsInline => false;

    public bool IsBlock => true;

    private readonly MarkdownContext _context;
    private readonly HtmlCodeSnippetRenderer _codeSnippetRenderer;

    public CodeExtension(MarkdownContext context)
    {
        _context = context;
        _codeSnippetRenderer = new(_context);
    }

    public bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning)
    {
        var block = (TripleColonBlock)markdownObject;
        block.Attributes.TryGetValue("id", out var currentId); // it's okay if this is null
        block.Attributes.TryGetValue("range", out var currentRange); // it's okay if this is null
        block.Attributes.TryGetValue("source", out var currentSource); // source has already been checked above
        var (code, codePath) = _context.ReadFile(currentSource, block);
        if (string.IsNullOrEmpty(code))
        {
            logWarning($"The code snippet \"{currentSource}\" could not be found.");
            return false;
        }

        HtmlCodeSnippetRenderer.TryGetLineRanges(currentRange, out var ranges);

        var snippet = new CodeSnippet(null)
        {
            CodePath = currentSource,
            TagName = currentId,
            CodeRanges = ranges,
        };

        var updatedCode = _codeSnippetRenderer.GetContent(code, snippet);
        updatedCode = ExtensionsHelper.Escape(updatedCode).TrimEnd();

        if (string.IsNullOrEmpty(updatedCode))
        {
            logWarning($"It looks like your code snippet was not rendered. Try range instead.");
            return false;
        }
        renderer.WriteLine("<pre>");
        renderer.Write("<code").WriteAttributes(block).Write(">");
        renderer.WriteLine(updatedCode);
        renderer.WriteLine("</code></pre>");

        return true;
    }

    public bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject)
    {
        htmlAttributes = null;
        var source = "";
        var highlight = "";
        var language = "";
        var interactive = "";

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
                case "id":
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

        if (string.IsNullOrEmpty(language))
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
        if (!string.IsNullOrEmpty(highlight))
        {
            htmlAttributes.AddProperty("highlight-lines", highlight);
        }

        return true;
    }

    public bool TryValidateAncestry(ContainerBlock container, Action<string> logError)
    {
        return true;
    }

    private static string InferLanguageFromFile(string source, Action<string> logError)
    {
        var fileExtension = Path.GetExtension(source);
        if (fileExtension == null)
        {
            logError("Language is not set, and your source has no file type. Cannot infer language.");
        }
        var language = HtmlCodeSnippetRenderer.GetLanguageByFileExtension(fileExtension);
        if (string.IsNullOrEmpty(language))
        {
            logError("Language is not set, and we could not infer language from the file type.");
        }
        return language;
    }
}
