// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class JsonTokenTreeRenderer
    {
        public ImmutableDictionary<string, string> Tokens { get; set; }

        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("NewLine", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Code(" + token.Lang + ")", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Heading" + level, childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Hr", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Blockquote", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += Render(renderer, (MarkdownListItemBlockToken) item);
            }
            return this.Insert(type, childContent, Type.NonLeaf);
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("li", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Html", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Paragraph", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return this.Insert("Text" + token.Content, this.GetSize(), Type.Leaf);
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
                headerContent += this.Insert("headerItem", childContent, Type.NonLeaf);
            }
            content += this.Insert("Header", headerContent, Type.NonLeaf);

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
                    rowContent += this.Insert("RowItem", childContent, Type.NonLeaf);
                }
                bodyContent += this.Insert("Row", rowContent, Type.NonLeaf);
            }
            content += this.Insert("Body", bodyContent, Type.NonLeaf);
            return this.Insert("Table", content, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("NonParagraph", childContent, Type.NonLeaf);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Escape" + token.Content, this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Link", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Image", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Strong", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Em", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineCode", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Del", childContent, Type.NonLeaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Tag", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Br", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineText(" + token.Content.Replace("\n", "\\n") + ")", this.GetSize(), Type.Leaf);
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return this.Insert("Ignore", this.GetSize(), Type.Leaf);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return this.Insert($"Raw(From{token.Rule.Name})", GetSize(), Type.Leaf);
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
            StringBuffer result = "{\"name\":\"";
            result += name;
            result += "\"";
            switch (tokenType)
            {
                case Type.NonLeaf:
                {
                    // If tokenContent is not empty ,should remove the last character(',')
                    if (tokenContent.GetLength() > 0 && tokenContent.EndsWith(','))
                    {
                        // TODO: add a method 'remove' of StringBuffer
                        string contenttemp = tokenContent;
                        tokenContent = contenttemp.Remove(contenttemp.Length - 1);
                    }
                    result += ",\"children\":[";
                    result += tokenContent;
                    result += "]},";
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

        #endregion
    }
}