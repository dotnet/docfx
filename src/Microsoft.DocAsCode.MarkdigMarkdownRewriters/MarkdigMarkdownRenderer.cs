// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigMarkdownRewriters
{
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.MarkdownLite;

    public class MarkdigMarkdownRenderer : DfmMarkdownRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer render, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            if (token.Rule is DfmXrefShortcutInlineRule)
            {
                return $"@\"{token.Href}\"";
            }

            return base.Render(render, token, context);
        }
    }
}
