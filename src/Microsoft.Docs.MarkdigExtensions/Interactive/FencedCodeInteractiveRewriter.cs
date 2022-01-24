// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class FencedCodeInteractiveRewriter : InteractiveBaseRewriter
{
    public override IMarkdownObject Rewrite(IMarkdownObject markdownObject)
    {
        if (markdownObject is FencedCodeBlock fencedCode && !string.IsNullOrEmpty(fencedCode.Info))
        {
            var attributes = fencedCode.GetAttributes();
            var language = GetLanguage(fencedCode.Info, out var isInteractive);

            if (string.IsNullOrEmpty(language) || !isInteractive)
            {
                return markdownObject;
            }

            attributes.AddProperty("data-interactive", WebUtility.HtmlEncode(language));
            UpdateFencedCodeLanguage(attributes, fencedCode.Info, language);
        }

        return markdownObject;
    }

    private static void UpdateFencedCodeLanguage(HtmlAttributes attributes, string originalLanguage, string updatedLanguage)
    {
        originalLanguage = Constants.FencedCodePrefix + originalLanguage;
        updatedLanguage = Constants.FencedCodePrefix + updatedLanguage;

        var index = attributes.Classes?.IndexOf(originalLanguage);
        if (index.HasValue && index.Value != -1)
        {
            attributes.Classes[index.Value] = WebUtility.HtmlEncode(updatedLanguage);
        }
        else
        {
            attributes.AddClass(WebUtility.HtmlEncode(updatedLanguage));
        }
    }
}
