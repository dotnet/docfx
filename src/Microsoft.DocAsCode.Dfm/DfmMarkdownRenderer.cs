// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmMarkdownRenderer : MarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            return string.IsNullOrEmpty(token.Title)
                    ? $"[!INCLUDE [{token.Name}]({token.Src})]"
                    : $"[!INCLUDE [{token.Name}]({token.Src} \"{token.Title}\")]";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            return string.IsNullOrEmpty(token.Title)
                    ? $"[!INCLUDE [{token.Name}]({token.Src})]\n\n"
                    : $"[!INCLUDE [{token.Name}]({token.Src} \"{token.Title}\")]\n\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return $"[!{token.NoteType}]\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = "---\n";
            content += token.Content;
            content += "\n---\n";
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmSectionBlockToken token, MarkdownBlockContext context)
        {
            return string.IsNullOrEmpty(token.Attributes)
                    ? "[!div]\n"
                    : $"[!div {token.Attributes}]\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmVideoBlockToken token, MarkdownBlockContext context)
        {
            return $"[!VIDEO {token.Link}]\n\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmTabGroupBlockToken token, MarkdownBlockContext context)
        {
            var result = StringBuffer.Empty;
            foreach (var item in token.Items)
            {
                result += "#### [";
                foreach (var contentItem in item.Title.Content.Tokens)
                {
                    result += renderer.Render(contentItem);
                }
                result += "](#tab/";
                result += item.Id;
                if (string.IsNullOrEmpty(item.Condition))
                {
                    result += "/";
                    result += item.Condition;
                }
                result += ")\n";

                foreach (var contentItem in item.Content.Content)
                {
                    result += renderer.Render(contentItem);
                }
            }

            result += "* * *\n";
            return result;
        }
    }
}
