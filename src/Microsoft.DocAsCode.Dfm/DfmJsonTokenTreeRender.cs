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
            return Insert(token, $"{ExposeTokenNameInDfm(token)}>{Escape(token.Href)}", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, ExposeTokenNameInDfm(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, ExposeTokenNameInDfm(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"{ExposeTokenNameInDfm(token)}>{Escape(token.Content)}");
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = StringBuffer.Empty;
            var splitTokens = DfmBlockquoteHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                var sectionToken = splitToken.Token as DfmSectionBlockToken;
                if (sectionToken != null)
                {
                    content += Insert(sectionToken, ExposeTokenNameInDfm(sectionToken));
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    continue;
                }

                var noteToken = splitToken.Token as DfmNoteBlockToken;
                if (noteToken != null)
                {
                    var type = noteToken.NoteType.ToUpper();
                    content += Insert(noteToken, type);
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    continue;
                }

                var videoToken = splitToken.Token as DfmVideoBlockToken;
                if (videoToken != null)
                {
                    content += Insert(videoToken, $"{ExposeTokenNameInDfm(videoToken)}>{videoToken.Link}");
                    continue;
                }

                foreach (var item in splitToken.InnerTokens)
                {
                    content += renderer.Render(item);
                }
            }
            return Insert(token, ExposeTokenNameInDfm(token), content);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, ExposeTokenNameInDfm(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"{ExposeTokenNameInDfm(token)}>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmVideoBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, ExposeTokenNameInDfm(token));
        }

        private string ExposeTokenNameInDfm(IMarkdownToken token)
        {
            var tokenName = ExposeTokenName(token);
            tokenName = TrimStringStart(tokenName, "Dfm");
            return tokenName;
        }
    }
}
