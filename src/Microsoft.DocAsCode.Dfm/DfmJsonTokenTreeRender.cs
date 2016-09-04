// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmJsonTokenTreeRender : JsonTokenTreeRenderer
    {
        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, $"Xref>{Escape(token.Href)}", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, "IncludeBlock");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, "IncludeInline");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"YamlHeader>{Escape(token.Content)}");
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            var splitTokens = DfmBlockquoteHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                if (splitToken.Token is DfmSectionBlockToken)
                {
                    content += Insert(splitToken.Token, "SectionBlock");
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                }
                else if (splitToken.Token is DfmNoteBlockToken)
                {
                    var noteToken = (DfmNoteBlockToken) splitToken.Token;
                    var type = noteToken.NoteType.ToUpper();
                    content += Insert(splitToken.Token, type);
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                }
                else if (splitToken.Token is DfmVideoBlockToken)
                {
                    var videoToken = splitToken.Token as DfmVideoBlockToken;
                    content += Insert(splitToken.Token, $"VideoBlock>{videoToken.Link}");
                }
                else
                {
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                }
            }
            return Insert(token, "BlockquoteBlock", content);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, "FencesBlock");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"NoteBlock>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmVideoBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, "VideBlockToken");
        }
    }
}
