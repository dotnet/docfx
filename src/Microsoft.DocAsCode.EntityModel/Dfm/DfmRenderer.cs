// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Utility;

    public class DfmRenderer : HtmlRenderer
    {
        private static readonly DocfxFlavoredIncHelper _inlineInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DocfxFlavoredIncHelper _blockInclusionHelper = new DocfxFlavoredIncHelper();
        private static readonly DfmCodeExtractor _dfmCodeExtractor = new DfmCodeExtractor();

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmXrefInlineToken token, MarkdownInlineContext context)
        {
            var href = token.Href == null ? string.Empty : $" href=\"{StringHelper.HtmlEncode(token.Href)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            return $"<xref{href}{title}>{name}</xref>";
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmIncludeBlockToken token, MarkdownBlockContext context)
        {
            var href = token.Src == null ? null : $"src=\"{StringHelper.HtmlEncode(token.Src)}\"";
            var name = token.Name == null ? null : StringHelper.HtmlEncode(token.Name);
            var title = token.Title == null ? null : $"title=\"{StringHelper.HtmlEncode(token.Title)}\"";
            var resolved = _blockInclusionHelper.Load(engine, token.Src, token.Raw, context, ((DfmEngine)engine.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmIncludeInlineToken token, MarkdownInlineContext context)
        {
            var resolved = _inlineInclusionHelper.Load(engine, token.Src, token.Raw, context, ((DfmEngine)engine.Engine).InternalMarkup);
            return resolved;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmYamlHeaderBlockToken token, MarkdownBlockContext context)
        {
            var content = token.Content == null ? string.Empty : StringHelper.HtmlEncode(token.Content);
            return $"<yamlheader>{content}</yamlheader>";
        }

        public override StringBuffer Render(IMarkdownRenderer engine, MarkdownBlockquoteBlockToken token, MarkdownBlockContext context)
        {
            StringBuffer content = string.Empty;
            var splitTokens = DfmRendererHelper.SplitBlockquoteTokens(token.Tokens);
            foreach (var splitToken in splitTokens)
            {
                if (splitToken.Token is DfmSectionBlockToken)
                {
                    var sectionToken = splitToken.Token as DfmSectionBlockToken;
                    content += $"<div{sectionToken.Attributes}>";
                    content += RenderTokens(engine, splitToken.InnerTokens.ToImmutableArray(), context, true, token.Rule);
                    content += "</div>\n";
                }
                else if (splitToken.Token is DfmNoteBlockToken)
                {
                    var noteToken = splitToken.Token as DfmNoteBlockToken;
                    content += $"<div class=\"{noteToken.NoteType}\"><h5>{noteToken.NoteType}</h5>" + RenderTokens(engine, splitToken.InnerTokens.ToImmutableArray(), context, true, token.Rule) + "</div>\n";
                }
                else
                {
                    content += "<blockquote>";
                    content += RenderTokens(engine, splitToken.InnerTokens.ToImmutableArray(), context, true, token.Rule);
                    content += "</blockquote>\n";
                }
            }
            return content;
        }

        public virtual StringBuffer Render(IMarkdownRenderer engine, DfmFencesBlockToken token, MarkdownBlockContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {context.GetFilePathStack().Peek()}";
                Logger.LogError(errorMessage);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, errorMessage);
            }

            try
            {
                // TODO: Valid REST and REST-i script.
                var fencesPath = ((RelativePath)token.Path).BasedOn((RelativePath)context.GetFilePathStack().Peek());
                var extractResult = _dfmCodeExtractor.ExtractFencesCode(token, fencesPath);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, extractResult.ErrorMessage, extractResult.FencesCodeLines);
            }
            catch (FileNotFoundException)
            {
                string errorMessage = $"Can not find reference {token.Path}";
                Logger.LogError(errorMessage);
                return DfmRendererHelper.GetRenderedFencesBlockString(token, errorMessage);
            }
        }
    }
}
