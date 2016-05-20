// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmRenderer : HtmlRenderer
    {
        private static readonly DocfxFlavoredIncHelper _inlineInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DocfxFlavoredIncHelper _blockInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DfmCodeExtractor _dfmCodeExtractor = new DfmCodeExtractor();

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            StringBuffer result = "<xref";
            result = AppendAttribute(result, "href", token.Href);
            result = AppendAttribute(result, "title", token.Title);
            result = AppendAttribute(result, "data-throw-if-not-resolved", token.ThrowIfNotResolved.ToString());
            result = AppendAttribute(result, "data-raw", token.SourceInfo.Markdown);
            
            result += ">";
            foreach (var item in token.Content)
            {
                result += renderer.Render(item);
            }

            result += "</xref>";
            return result;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var resolved = _blockInclusionHelper.Load(renderer, token.Src, token.Raw, context, ((DfmEngine)renderer.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var resolved = _inlineInclusionHelper.Load(renderer, token.Src, token.Raw, context, ((DfmEngine)renderer.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            if (string.IsNullOrEmpty(token.Content))
            {
                return StringBuffer.Empty;
            }
            StringBuffer result = "<yamlheader>";
            result += StringHelper.HtmlEncode(token.Content);
            return result + "</yamlheader>";
        }

        public override StringBuffer Render(IMarkdownRenderer renderer, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = string.Empty;
            var splitTokens = DfmBlockquoteHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                if (splitToken.Token is DfmSectionBlockToken)
                {
                    content += "<div";
                    content += ((DfmSectionBlockToken)splitToken.Token).Attributes;
                    content += ">";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else if (splitToken.Token is DfmNoteBlockToken)
                {
                    var noteToken = (DfmNoteBlockToken)splitToken.Token;
                    content += "<div class=\"";
                    content += noteToken.NoteType.ToUpper();
                    content += "\"><h5>";
                    content += noteToken.NoteType.ToUpper();
                    content += "</h5>";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</div>\n";
                }
                else
                {
                    content += "<blockquote>";
                    foreach (var item in splitToken.InnerTokens)
                    {
                        content += renderer.Render(item);
                    }
                    content += "</blockquote>\n";
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {context.GetFilePathStack().Peek()}";
                Logger.LogError(errorMessage);
                return DfmFencesBlockHelper.GetRenderedFencesBlockString(token, renderer.Options, errorMessage);
            }

            try
            {
                // TODO: Valid REST and REST-i script.
                var fencesPath = Path.Combine(context.GetBaseFolder(), (RelativePath)context.GetFilePathStack().Peek() + (RelativePath)token.Path);
                var extractResult = _dfmCodeExtractor.ExtractFencesCode(token, fencesPath);
                return DfmFencesBlockHelper.GetRenderedFencesBlockString(token, renderer.Options, extractResult.ErrorMessage, extractResult.FencesCodeLines);
            }
            catch (DirectoryNotFoundException)
            {
                return DfmFencesBlockHelper.GenerateReferenceNotFoundErrorMessage(renderer, token);
            }
            catch (FileNotFoundException)
            {
                return DfmFencesBlockHelper.GenerateReferenceNotFoundErrorMessage(renderer, token);
            }
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmNoteBlockToken token, MarkdownBlockContext context)
        {
            return token.Content;
        }

        private static StringBuffer AppendAttribute(StringBuffer buffer, string attributeName, string value)
        {
            if (string.IsNullOrEmpty(value)) return buffer;
            buffer += " ";
            buffer += attributeName;
            buffer += "=\"";
            buffer += StringHelper.HtmlEncode(value);
            buffer += "\"";
            return buffer;
        }
    }
}
