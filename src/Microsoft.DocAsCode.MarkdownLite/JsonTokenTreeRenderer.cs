// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class JsonTokenTreeRenderer
    {
        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert($"NewLine_{token.SourceInfo.LineNumber}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            if (token.Lang != null)
            {
                return this.Insert($"Code({token.Lang})_{token.SourceInfo.LineNumber}_{this.escape(token.Code)}", this.GetSize(), Type.Leaf);
            }
            else
            {
                return this.Insert($"Code_{token.SourceInfo.LineNumber}_{this.escape(token.Code)}", this.GetSize(), Type.Leaf);
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
            return this.Insert($"Heading{level}_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert($"Hr_{token.SourceInfo.LineNumber}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Blockquote_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += Render(renderer, (MarkdownListItemBlockToken) item);
            }
            return this.Insert(type + "_" + token.SourceInfo.LineNumber.ToString(), childContent, Type.NonLeaf);
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"li_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Html_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Paragraph_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return this.Insert($"Text_{token.SourceInfo.LineNumber}_{this.escape(token.Content)}", this.GetSize(), Type.Leaf);
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
                headerContent += this.Insert($"headerItem_{token.Header[i].SourceInfo.LineNumber}", childContent, Type.NonLeaf);
            }
            content += this.Insert($"Header_{token.SourceInfo.LineNumber}", headerContent, Type.NonLeaf);

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
                    rowContent += this.Insert($"RowItem_{row[j].SourceInfo.LineNumber}", childContent, Type.NonLeaf);
                }
                bodyContent += this.Insert($"Row_{row[0].SourceInfo.LineNumber}", rowContent, Type.NonLeaf);
            }
            content += this.Insert($"Body_{token.SourceInfo.LineNumber + 1}", bodyContent, Type.NonLeaf);
            return this.Insert($"Table_{token.SourceInfo.LineNumber}", content, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"NonParagraph_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert($"Escape_{token.SourceInfo.LineNumber}_{this.escape(token.Content)}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Link_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert($"Image_{token.SourceInfo.LineNumber}_{this.escape(token.Href)}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Strong_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Em_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert($"InLineCode_{token.SourceInfo.LineNumber}_{this.escape(token.Content)}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert($"Del_{token.SourceInfo.LineNumber}", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert($"Tag_{token.SourceInfo.LineNumber}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert($"Br_{token.SourceInfo.LineNumber}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return this.Insert($"InLineText_{token.SourceInfo.LineNumber}_{this.escape(token.Content)}", this.GetSize(), Type.Leaf);
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return this.Insert($"Ignore_{token.SourceInfo.LineNumber}_{this.escape(token.SourceInfo.Markdown)}", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return this.Insert($"Raw(From{token.Rule.Name})_{token.SourceInfo.LineNumber}_{this.escape(token.SourceInfo.Markdown)}", GetSize(), Type.Leaf);
        }

        #endregion

        #region Private

        private enum Type
        {
            Leaf,
            NonLeaf
        }

        private StringBuffer Insert(StringBuffer name, StringBuffer tokenContent, Type tokenType)
        {
            StringBuffer result = $"{{\"name\":\"{name}\"";
            switch (tokenType)
            {
                case Type.NonLeaf:
                {
                    // If tokenContent is not empty ,should remove the last character(',')
                    if (tokenContent.GetLength() > 0 && tokenContent.EndsWith(','))
                    {
                        // TODO: add a method 'remove' of StringBuffer
                        string contentTemp = tokenContent;
                        tokenContent = contentTemp.Remove(contentTemp.Length - 1);
                    }
                    result += $",\"children\":[{tokenContent}]}},";
                    break;
                }
                case Type.Leaf:
                {
                    // Leaf will be needed later
                    // TODO: result += "\"size\":" + tokenContent + "\n},";
                    result += "},";
                    break;
                }
                default:
                    return StringBuffer.Empty;
            }
            return result;
        }

        private string GetSize()
        {
            // TODO: get the node size
            return "1";
        }

        private string escape(string content)
        {
            return content.Replace("\n", "\\n")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;");
        }

        #endregion
    }
}