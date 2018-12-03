// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmCodeRenderer
    {
        private readonly DfmCodeExtractor _dfmCodeExtractor;

        public DfmCodeRenderer()
            : this((DfmCodeExtractor)null)
        {
        }

        public DfmCodeRenderer(DfmCodeExtractor extractor)
        {
            _dfmCodeExtractor = extractor ?? new DfmCodeExtractor();
        }

        public DfmCodeRenderer(CodeLanguageExtractorsBuilder builder)
        {
            _dfmCodeExtractor = new DfmCodeExtractor(builder);
        }

        public virtual StringBuffer Render(IMarkdownRenderer renderer, DfmFencesToken token, IMarkdownContext context)
        {
            if (!PathUtility.IsRelativePath(token.Path))
            {
                string errorMessage = $"Code absolute path: {token.Path} is not supported in file {context.GetFilePathStack().Peek()}";
                Logger.LogError(errorMessage);
                return RenderFencesCode(token, renderer.Options, errorMessage);
            }

            try
            {
                // Always report original dependency when path is valid
                if (PathUtility.IsVaildFilePath(token.Path))
                {
                    context.ReportDependency(token.Path);
                }
                
                var pathQueryOption =
                    !string.IsNullOrEmpty(token.QueryStringAndFragment) ?
                    _dfmCodeExtractor.ParsePathQueryString(token.QueryStringAndFragment) :
                    null;
                var filePath = FindFile(token, context);
                var code = ExtractCode(token, filePath, pathQueryOption);
                return RenderFencesCode(token, renderer.Options, code.ErrorMessage, code.CodeLines, pathQueryOption);
            }
            catch (DirectoryNotFoundException)
            {
                return RenderReferenceNotFoundErrorMessage(renderer, token);
            }
            catch (FileNotFoundException)
            {
                return RenderReferenceNotFoundErrorMessage(renderer, token);
            }
        }

        [Obsolete]
        public virtual StringBuffer RenderFencesFromCodeContent(string codeContent, string path, string queryStringAndFragment = null, string name = null, string lang = null, string title = null)
        {
            if (codeContent == null)
            {
                return RenderCodeErrorString($"{nameof(codeContent)} can not be null");
            }

            if (string.IsNullOrEmpty(path))
            {
                return RenderCodeErrorString($"{nameof(path)} can not been null or empty");
            }

            if (queryStringAndFragment != null && queryStringAndFragment.Length == 1)
            {
                return RenderCodeErrorString($"Length of {nameof(queryStringAndFragment)} can not be 1");
            }

            var pathQueryOption =
                !string.IsNullOrEmpty(queryStringAndFragment)
                    ? DfmFencesRule.ParsePathQueryString(queryStringAndFragment.Remove(1), queryStringAndFragment.Substring(1), true)
                    : null;

            var token = new DfmFencesBlockToken(null, null, name, path, new SourceInfo(), lang, title, pathQueryOption, queryStringAndFragment);

            var fencesCode = codeContent.Replace("\r\n", "\n").Split('\n');
            var code = ExtractCode(token, fencesCode);
            return RenderFencesCode(token, new Options { ShouldExportSourceInfo = false }, code.ErrorMessage, code.CodeLines);
        }

        public virtual StringBuffer RenderFencesFromCodeContent(string codeContent, DfmFencesBlockToken token)
        {
            if (codeContent == null)
            {
                return RenderCodeErrorString($"{nameof(codeContent)} can not be null");
            }

            if (string.IsNullOrEmpty(token.Path))
            {
                return RenderCodeErrorString($"{nameof(token.Path)} can not been null or empty");
            }

            if (token.QueryStringAndFragment != null && token.QueryStringAndFragment.Length == 1)
            {
                return RenderCodeErrorString($"Length of {nameof(token.QueryStringAndFragment)} can not be 1");
            }

            var fencesCode = codeContent.Replace("\r\n", "\n").Split('\n');

            var pathQueryOption = _dfmCodeExtractor.ParsePathQueryString(token.QueryStringAndFragment);

            var code = ExtractCode(token, fencesCode, pathQueryOption);
            return RenderFencesCode(token, new Options { ShouldExportSourceInfo = false }, code.ErrorMessage, code.CodeLines, pathQueryOption);
        }

        public virtual string FindFile(DfmFencesToken token, IMarkdownContext context)
        {
            return DfmFallbackHelper.GetFilePathWithFallback(token.Path, context).Item1;
        }

        [Obsolete]
        public virtual DfmExtractCodeResult ExtractCode(DfmFencesToken token, string filePath)
            => ExtractCode(token, filePath, null);

        public virtual DfmExtractCodeResult ExtractCode(DfmFencesToken token, string filePath, IDfmFencesBlockPathQueryOption option)
        {
            return _dfmCodeExtractor.ExtractFencesCode(token, filePath, option);
        }

        [Obsolete]
        public virtual DfmExtractCodeResult ExtractCode(DfmFencesToken token, string[] fencesCode)
        {
            return _dfmCodeExtractor.ExtractFencesCode(token, fencesCode, null);
        }

        public virtual DfmExtractCodeResult ExtractCode(DfmFencesToken token, string[] fencesCode, IDfmFencesBlockPathQueryOption option)
        {
            return _dfmCodeExtractor.ExtractFencesCode(token, fencesCode, option);
        }

        public virtual StringBuffer RenderFencesCode(DfmFencesToken token,
            Options options,
            string errorMessage,
            string[] codeLines = null,
            IDfmFencesBlockPathQueryOption pathQueryOption = null)
        {
            StringBuffer result;
            string renderedErrorMessage = string.Empty;
            string renderedCodeLines = string.Empty;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                result = RenderCodeErrorString(errorMessage);
            }
            else
            {
                result = StringBuffer.Empty;
            }

            if (codeLines != null)
            {
                result = RenderOpenPreTag(result, token, options);
                result = RenderOpenCodeTag(result, token, options, pathQueryOption);
                foreach (var line in codeLines)
                {
                    result += StringHelper.HtmlEncode(line);
                    result += "\n";
                }
                result = RenderCloseCodeTag(result, token, options);
                result = RenderClosePreTag(result, token, options);
            }

            return result;
        }

        public virtual StringBuffer RenderOpenPreTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            result += "<pre";
            result = HtmlRenderer.AppendSourceInfo(result, options, token);
            return result + ">";
        }

        public virtual StringBuffer RenderClosePreTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return result + "</pre>";
        }

        [Obsolete]
        public virtual StringBuffer RenderOpenCodeTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return RenderOpenCodeTag(result, token, options, token.PathQueryOption);
        }

        public virtual StringBuffer RenderOpenCodeTag(StringBuffer result, DfmFencesToken token, Options options, IDfmFencesBlockPathQueryOption pathQueryOption)
        {
            result += "<code";
            if (!string.IsNullOrEmpty(token.Lang))
            {
                result = result + " class=\"" + options.LangPrefix + token.Lang + "\"";
            }
            if (!string.IsNullOrEmpty(token.Name))
            {
                result = result + " name=\"" + StringHelper.HtmlEncode(token.Name) + "\"";
            }
            if (!string.IsNullOrEmpty(token.Title))
            {
                result = result + " title=\"" + StringHelper.HtmlEncode(token.Title) + "\"";
            }
            if (!string.IsNullOrEmpty(pathQueryOption?.HighlightLines))
            {
                result = result + " highlight-lines=\"" + StringHelper.HtmlEncode(pathQueryOption.HighlightLines) + "\"";
            }
            result += ">";
            return result;
        }

        public virtual StringBuffer RenderCloseCodeTag(StringBuffer result, DfmFencesToken token, Options options)
        {
            return result + "</code>";
        }

        public virtual StringBuffer RenderReferenceNotFoundErrorMessage(IMarkdownRenderer renderer, DfmFencesToken token)
        {
            var errorMessageInMarkdown = $"Can not find reference {token.Path}";
            var errorMessage = $"Unable to resolve {token.SourceInfo.Markdown}. {errorMessageInMarkdown}.";
            Logger.LogWarning(errorMessage, line: token.SourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.InvalidCodeSnippet);
            return RenderCodeErrorString(errorMessageInMarkdown);
        }

        public virtual StringBuffer RenderCodeErrorString(string errorMessage)
        {
            return (StringBuffer)"<!-- " + StringHelper.HtmlEncode(errorMessage) + " -->\n";
        }
    }
}
