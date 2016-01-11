// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters
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
                    ? $"[!INCLUDE [{token.Name}]({token.Src})]\n"
                    : $"[!INCLUDE [{token.Name}]({token.Src} \"{token.Title}\")]\n";
        }

        public virtual StringBuffer Render(IMarkdownRenderer render, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return $"[!{token.NoteType}]\n";
        }
    }
}
