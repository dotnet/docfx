// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Immutable;
    using System.Text;

    using Microsoft.DocAsCode.MarkdownLite;

    public class JsonRenderer : HtmlRenderer
    {
        public enum Type
        {
            size,
            child
        }

        public ImmutableDictionary<string, string> Tokens { get; set; }

        public string Insert(string name, string tokenContent, Type tokentype)
        {
            var result = (StringBuffer)"{\n\"name\":\"" + name + "\"";
            if (tokentype == Type.child)
            {
                // If tokenContent is not empty ,should remove the last character(',')
                if (tokenContent.Length > 0)
                {
                    tokenContent = tokenContent.Remove(tokenContent.Length - 1);
                }

                result += ",\n\"contents\":[\n" + tokenContent + "]\n},";
            }
            else if (tokentype == Type.size)
            {
                // size will be need later
                // result += "\"size\":" + tokenContent + "\n},";
                result += "\n},";
            }
            return result;
        }

        public string GetSize()
        {
            return "1";
        }

        #region Block

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNewLineBlockToken token, MarkdownBlockContext context)
        {
            // do nothing.
            return StringBuffer.Empty;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Code(" + token.Lang + ")", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHeadingBlockToken token, MarkdownBlockContext context)
        {
            string level = token.Depth.ToString();
            var childContent = new StringBuilder();
            foreach (var item in token.Content.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Heading" + level, childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHrBlockToken token, MarkdownBlockContext context)
        {
            return this.Insert("Hr", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Blockquote", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListBlockToken token, MarkdownBlockContext context)
        {
            var type = token.Ordered ? "ol" : "ul";
            var childContent = new StringBuilder();
            foreach (var item in token.Tokens)
            {
                childContent.Append(Render(renderer, (MarkdownListItemBlockToken)item));
            }
            return this.Insert(type, childContent.ToString(), Type.child);
        }

        protected virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownListItemBlockToken token)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("li", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownHtmlBlockToken token, MarkdownBlockContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Html", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.InlineTokens.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Paragraph", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownBlockContext context)
        {
            return this.Insert("Text:" + token.Content, this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTableBlockToken token, MarkdownBlockContext context)
        {
            var content = new StringBuilder();

            // header
            var headerContent = new StringBuilder();
            for (int i = 0; i < token.Header.Length; i++)
            {
                var childContent = new StringBuilder();
                foreach (var item in token.Header[i].Tokens)
                {
                    childContent.Append(renderer.Render(item));
                }
                headerContent.Append(this.Insert("headerItem", childContent.ToString(), Type.child));
            }
            content.Append(this.Insert("Header", headerContent.ToString(), Type.child));

            // Body
            var bodyContent = new StringBuilder();
            var rowContent = new StringBuilder();
            for (int i = 0; i < token.Cells.Length; i++)
            {
                rowContent.Clear();
                var row = token.Cells[i];
                for (int j = 0; j < row.Length; j++)
                {
                    var childContent = new StringBuilder();
                    foreach (var item in row[j].Tokens)
                    {
                        childContent.Append(renderer.Render(item));
                    }
                    rowContent.Append(this.Insert("RowItem", childContent.ToString(), Type.child));
                }
                bodyContent.Append(this.Insert("Row", rowContent.ToString(), Type.child));
            }
            content.Append(this.Insert("Body", bodyContent.ToString(), Type.child));
            return this.Insert("Table", content.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownNonParagraphBlockToken token, MarkdownBlockContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content.Tokens)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("NonParagraph", childContent.ToString(), Type.child);
        }

        #endregion

        #region Inline

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEscapeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Escape:" + token.Content, this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownLinkInlineToken token, MarkdownInlineContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Link", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownImageInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Image", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownStrongInlineToken token, MarkdownInlineContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Strong", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownEmInlineToken token, MarkdownInlineContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Em", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownCodeInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineCode", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, GfmDelInlineToken token, MarkdownInlineContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Del", childContent.ToString(), Type.child);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTagInlineToken token, MarkdownInlineContext context)
        {
            if (renderer.Options.Sanitize)
            {
                if (renderer.Options.Sanitizer != null)
                {
                    return renderer.Options.Sanitizer(token.SourceInfo.Markdown);
                }
                return StringHelper.Escape(token.SourceInfo.Markdown);
            }
            return token.SourceInfo.Markdown;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownBrInlineToken token, MarkdownInlineContext context)
        {
            return this.Insert("Br", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownTextToken token, MarkdownInlineContext context)
        {
            return this.Insert("InLineText(" + token.Content.Replace("\n", "\\n") + ")", this.GetSize(), Type.size);
        }

        #endregion

        #region Misc

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownIgnoreToken token, IMarkdownContext context)
        {
            return this.Insert("Ignore", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, MarkdownRawToken token, IMarkdownContext context)
        {
            return this.Insert("Raw", this.GetSize(), Type.size);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            var childContent = new StringBuilder();
            foreach (var item in token.Content)
            {
                childContent.Append(renderer.Render(item));
            }
            return this.Insert("Xref", childContent.ToString(), Type.child);
        }

        #endregion
    }
}