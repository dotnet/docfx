// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class JsonRenderer
    {
        public enum Type
        {
            Size,
            Child
        }

        public ImmutableDictionary<string, string> Tokens { get; set; }

        private StringBuffer Insert(string name, string tokenContent, Type tokentype)
        {
            var result = (StringBuffer)$"{{\n\"name\":\"{name}\"";
            switch (tokentype)
            {
                case Type.Child:
                    {
                        // If tokenContent is not empty ,should remove the last character(',')
                        if (tokenContent.Length > 0)
                        {
                            tokenContent = tokenContent.Remove(tokenContent.Length - 1);
                        }
                        result += $",\n\"contents\":[\n{tokenContent}]\n}},";
                        break;
                    }
                case Type.Size:
                    {
                        // size will be needed later
                        // result += "\"size\":" + tokenContent + "\n},";
                        result += "\n},";
                        break;
                    }
                default:
                    return StringBuffer.Empty;
            }
            return result;
        }

        private string GetSize()
        {
            return "1";
        }

        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            // do nothing.
            return this.Insert("NewLine", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Code(" + token.Lang + ")", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Heading" + level, childContent, Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Hr", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Blockquote", childContent, Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += Render(renderer, (MarkdownListItemBlockToken) item);
            }
            return this.Insert(type, childContent, Type.Child);
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("li", childContent, Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Html", childContent.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Paragraph", childContent, Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return this.Insert("Text:" + token.Content, this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            var content = StringBuffer.Empty;

            // header
            var headerContent = StringBuffer.Empty;
            for (int i = 0; i < token.Header.Length; i++)
            {
                var childContent = StringBuffer.Empty;
                foreach (var item in token.Header[i].Tokens)
                {
                    childContent += renderer.Render(item);
                }
                headerContent += this.Insert("headerItem", childContent.ToString(), Type.Child);
            }
            content += this.Insert("Header", headerContent.ToString(), Type.Child);

            // Body
            var bodyContent = StringBuffer.Empty;
            for (int i = 0; i < token.Cells.Length; i++)
            {
                var rowContent = StringBuffer.Empty;
                var row = token.Cells[i];
                for (int j = 0; j < row.Length; j++)
                {
                    var childContent = StringBuffer.Empty;
                    foreach (var item in row[j].Tokens)
                    {
                        childContent += renderer.Render(item);
                    }
                    rowContent += this.Insert("RowItem", childContent.ToString(), Type.Child);
                }
                bodyContent += this.Insert("Row", rowContent.ToString(), Type.Child);
            }
            content += this.Insert("Body", bodyContent.ToString(), Type.Child);
            return this.Insert("Table", content.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content.Tokens)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("NonParagraph", childContent.ToString(), Type.Child);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Escape:" + token.Content, this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Link", childContent.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Image", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Strong", childContent.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Em", childContent.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineCode", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = StringBuffer.Empty;
            foreach (var item in token.Content)
            {
                childContent += renderer.Render(item);
            }
            return this.Insert("Del", childContent.ToString(), Type.Child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Tag", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Br", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineText(" + token.Content.Replace("\n", "\\n") + ")", this.GetSize(), Type.Size);
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return this.Insert("Ignore", this.GetSize(), Type.Size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return this.Insert("Raw", this.GetSize(), Type.Size);
        }

        #endregion
    }
}