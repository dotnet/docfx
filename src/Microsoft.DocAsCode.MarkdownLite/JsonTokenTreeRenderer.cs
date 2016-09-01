// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class JsonTokenTreeRenderer
    {
        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, "Newline");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            if (token.Lang != null)
            {
                return Insert(token, $"Code({token.Lang})>{Escape(token.Code)}");
            }
            else
            {
                return Insert(token, $"Code>{Escape(token.Code)}");
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
            return Insert(token, $"Heading{level}", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return Insert(token, "Hr");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, $"Blockquote", childContent);
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
            return Insert(token, "li", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Html", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Paragraph", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return Insert(token, $"Text{Escape(token.Content)}");
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
            return Insert(token, "Table", content);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "NonParagraph", childContent);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"Escape>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Link", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"Image>{Escape(token.Href)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Strong", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Em", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"InLineCode>{Escape(token.Content)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return Insert(token, "Del", childContent);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, "Tag");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return Insert(token, "Br");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return Insert(token, $"InLineText>{Escape(token.Content)}");
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return Insert(token, $"Ignore>{Escape(token.SourceInfo.Markdown)}");
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return Insert(token, $"Raw(From{token.Rule.Name})>{Escape(token.SourceInfo.Markdown)}");
        }

        #endregion

        #region Private

        protected StringBuffer Insert(IMarkdownToken token, StringBuffer name, StringBuffer tokenContent = null)
        {
            // TODO: separate the name to extra properties
            int startLineNumber = token.SourceInfo.LineNumber;
            int endLineNumber = (token.SourceInfo.ValidLineCount > 0) ? (startLineNumber + token.SourceInfo.ValidLineCount - 1) : startLineNumber;
            StringBuffer result = $"{{\"name\":\"{startLineNumber}>{endLineNumber}>{name}\"";
            if (tokenContent != null)
            {
                // If tokenContent is not empty ,should remove the last character(',')
                if (tokenContent.GetLength() > 0 && tokenContent.EndsWith(','))
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

        protected StringBuffer Insert(int startLineNumber, int endLineNumber, StringBuffer name, StringBuffer tokenContent = null)
        {
            StringBuffer result = $"{{\"name\":\"{startLineNumber}>{endLineNumber}>{name}\"";
            if (tokenContent != null)
            {
                // If tokenContent is not empty ,should remove the last character(',')
                if (tokenContent != StringBuffer.Empty && tokenContent.EndsWith(','))
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

        #endregion
    }
}