// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    public class JsonTokenTreeRenderer
    {
        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            if (!string.IsNullOrEmpty(token.Lang))
            {
                return Insert(token, $"{ExposeTokenName(token)}({token.Lang})>{Escape(token.Code)}");
            }
            else
            {
                return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Code)}");
            }
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, $"{ExposeTokenName(token)}{level}", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, ExposeTokenName(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += Render(renderer, (MarkdownListItemBlockToken) item);
            }
            return Insert(token, type, childContent);
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;

            // Header
            var headerContent = StringBuffer.Empty;
            for (int i = 0; i < token.Header.Length; i++)
            {
                var childContent = StringBuffer.Empty;
                foreach (var item in token.Header[i].Content.Tokens)
                {
                    childContent += renderer.Render(item);
                }
                headerContent += Insert(token.Header[i], "headerItem", childContent);
            }
            content += Insert(token.SourceInfo.LineNumber, token.SourceInfo.LineNumber, "Header", headerContent);

            // Body
            var bodyContent = StringBuffer.Empty;
            for (int i = 0; i < token.Cells.Length; i++)
            {
                var rowContent = StringBuffer.Empty;
                var row = token.Cells[i];
                for (int j = 0; j < row.Length; j++)
                {
                    var childContent = StringBuffer.Empty;
                    foreach (var item in row[j].Content.Tokens)
                    {
                        childContent += renderer.Render(item);
                    }
                    rowContent += Insert(row[j], "RowItem", childContent);
                }
                bodyContent += Insert(row[0], "Row", rowContent);
            }
            content += Insert(token.Cells[0][0].SourceInfo.LineNumber, token.Cells[token.Cells.Length - 1][0].SourceInfo.LineNumber, "Body", bodyContent);
            return Insert(token, ExposeTokenName(token), content);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Href)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, ExposeTokenName(token), childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, ExposeTokenName(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, ExposeTokenName(token));
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.Content)}");
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}>{Escape(token.SourceInfo.Markdown)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return Insert(token, $"{ExposeTokenName(token)}(From{token.Rule.Name})>{Escape(token.SourceInfo.Markdown)}");
        }

        #endregion

        #region Protected

        protected StringBuffer Insert(IMarkdownToken token, StringBuffer name, StringBuffer tokenContent = null)
        {
            // TODO: separate the name to extra properties
            int startLineNumber = token.SourceInfo.LineNumber;
            int endLineNumber = (token.SourceInfo.ValidLineCount > 0) ? (startLineNumber + token.SourceInfo.ValidLineCount - 1) : startLineNumber;
            return Insert(startLineNumber, endLineNumber, name, tokenContent);
        }

        protected StringBuffer Insert(int startLineNumber, int endLineNumber, StringBuffer name, StringBuffer tokenContent = null)
        {
            StringBuffer result = $"{{\"name\":\"{startLineNumber}>{endLineNumber}>{name}\"";
            if (tokenContent != null)
            {
                // If tokenContent is not empty ,should remove the last character(',')
                if (tokenContent.EndsWith(','))
                {
                    // TODO: add a method 'remove' of StringBuffer
                    string contentTemp = tokenContent;
                    tokenContent = contentTemp.Remove(contentTemp.Length - 1);
                }
                result += $",\"children\":[{tokenContent}]";
            }
            result += "},";

            return result;
        }

        protected string Escape(string content)
        {
            // TODO: reuse the StringHelper.HtmlEncode
            return content.Replace("\n", "\\n")
                .Replace("\r" , "\\r")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;");
        }

        protected string ExposeTokenName(IMarkdownToken token)
        {
            var tokenName = token.GetType().Name;
            tokenName = TrimStringStart(tokenName, "Markdown");
            tokenName = TrimStringStart(tokenName, "Gfm");
            tokenName = TrimStringEnd(tokenName, "Token");
            tokenName = TrimStringEnd(tokenName, "Block");
            tokenName = TrimStringEnd(tokenName, "Inline");
            return tokenName;
        }

        protected string TrimStringStart(string source, string target)
        {
            if (source.StartsWith(target, StringComparison.Ordinal))
            {
                return source.Substring(target.Length, source.Length - target.Length);
            }
            return source;
        }

        protected string TrimStringEnd(string source, string target)
        {
            if (source.EndsWith(target, StringComparison.Ordinal))
            {
                return source.Substring(0, source.Length - target.Length);
            }
            return source;
        }

        #endregion
    }
}