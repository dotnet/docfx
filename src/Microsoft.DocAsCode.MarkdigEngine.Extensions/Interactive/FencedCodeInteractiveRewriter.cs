// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Net;

    using Markdig.Renderers.Html;
    using Markdig.Syntax;

    public class FencedCodeInteractiveRewriter : InteractiveBaseRewriter
    {
        public override IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            if (markdownObject is FencedCodeBlock fencedCode)
            {
                var attributes = fencedCode.GetAttributes();
                var language = GetLanguage(fencedCode.Info, out bool isInteractive);

                if (!string.IsNullOrEmpty(language))
                {
                    attributes.AddProperty("data-interactive", WebUtility.HtmlEncode(language));

                    var originalLanguage = Constants.FencedCodePrefix + fencedCode.Info;
                    var updatedLanguage = Constants.FencedCodePrefix + language;

                    if (attributes.Classes != null)
                    {
                        var index = attributes.Classes.IndexOf(originalLanguage);
                        if (index != -1)
                        {
                            attributes.Classes[index] = updatedLanguage;
                        }
                        else
                        {
                            attributes.AddClass(updatedLanguage);
                        }
                    }
                    else
                    {
                        attributes.AddClass(updatedLanguage);
                    }
                }
            }

            return markdownObject;
        }
    }
}
