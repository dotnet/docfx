// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    [Obsolete]
    public static class DfmFencesBlockHelper
    {
        [Obsolete]
        public static string GetRenderedFencesBlockString(DfmFencesBlockToken token, Options options,
            string errorMessage, string[] codeLines = null)
            => GetRenderedFencesBlockString((DfmFencesToken)token, options, errorMessage, codeLines);

        [Obsolete]
        public static string GetRenderedFencesBlockString(DfmFencesToken token, Options options, string errorMessage, string[] codeLines = null)
        {
            string renderedErrorMessage = string.Empty;
            string renderedCodeLines = string.Empty;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                renderedErrorMessage = $"<!-- {StringHelper.HtmlEncode(errorMessage)} -->\n";
            }

            if (codeLines != null)
            {
                var lang = string.IsNullOrEmpty(token.Lang) ? null : $" class=\"{options.LangPrefix}{token.Lang}\"";
                var name = string.IsNullOrEmpty(token.Name) ? null : $" name=\"{StringHelper.HtmlEncode(token.Name)}\"";
                var title = string.IsNullOrEmpty(token.Title) ? null : $" title=\"{StringHelper.HtmlEncode(token.Title)}\"";
                var highlight = string.IsNullOrEmpty(token.PathQueryOption?.HighlightLines)
                                ? null
                                : $" highlight-lines=\"{StringHelper.HtmlEncode(token.PathQueryOption.HighlightLines)}\"";

                renderedCodeLines = $"<pre><code{lang}{name}{title}{highlight}>{StringHelper.HtmlEncode(string.Join("\n", codeLines))}\n</code></pre>";
            }

            return $"{renderedErrorMessage}{renderedCodeLines}";
        }

        [Obsolete]
        public static string GenerateReferenceNotFoundErrorMessage(IMarkdownRenderer renderer, DfmFencesBlockToken token)
            => GenerateReferenceNotFoundErrorMessage(renderer, (DfmFencesToken)token);

        [Obsolete]
        public static string GenerateReferenceNotFoundErrorMessage(IMarkdownRenderer renderer, DfmFencesToken token)
        {
            var errorMessageInMarkdown = $"Can not find reference {token.Path}";
            var errorMessage = $"Unable to resolve {token.SourceInfo.Markdown}. {errorMessageInMarkdown}.";
            Logger.LogError(errorMessage, line: token.SourceInfo.LineNumber.ToString());
            return GetRenderedFencesBlockString(token, renderer.Options, errorMessageInMarkdown);
        }
    }
}
