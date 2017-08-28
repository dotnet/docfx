// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class DfmCodeExtractor
    {
        private static readonly string RemoveIndentSpacesRegexString = @"^[ \t]{{1,{0}}}";

        public DfmExtractCodeResult ExtractFencesCode(DfmFencesToken token, string fencesPath)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrEmpty(fencesPath))
            {
                throw new ArgumentNullException(nameof(fencesPath));
            }

            using (new LoggerPhaseScope("Extract Dfm Code"))
            {
                var fencesCode = EnvironmentContext.FileAbstractLayer.ReadAllLines(fencesPath);

                return ExtractFencesCodeCore(token, fencesCode);
            }
        }

        public DfmExtractCodeResult ExtractFencesCode(DfmFencesToken token, string[] fencesCode)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (fencesCode == null)
            {
                throw new ArgumentNullException(nameof(fencesCode));
            }

            using (new LoggerPhaseScope("Extract Dfm Code"))
            {
                return ExtractFencesCodeCore(token, fencesCode);
            }
        }

        private DfmExtractCodeResult ExtractFencesCodeCore(DfmFencesToken token, string[] fencesCode)
        {
            if (token.PathQueryOption == null)
            {
                // Add the full file when no query option is given
                return new DfmExtractCodeResult
                {
                    IsSuccessful = true,
                    CodeLines = Dedent(fencesCode),
                };
            }

            if (!token.PathQueryOption.ValidateAndPrepare(fencesCode, token))
            {
                Logger.LogWarning(GenerateErrorMessage(token), line: token.SourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.InvalidCodeSnippet);
                return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = token.PathQueryOption.ErrorMessage, CodeLines = fencesCode };
            }
            if (!string.IsNullOrEmpty(token.PathQueryOption.ErrorMessage))
            {
                Logger.LogWarning(GenerateErrorMessage(token), line: token.SourceInfo.LineNumber.ToString());
            }

            var includedLines = token.PathQueryOption.GetQueryLines(fencesCode).ToList();

            if (!token.PathQueryOption.ValidateHighlightLinesAndDedentLength(includedLines.Count))
            {
                Logger.LogWarning(GenerateErrorMessage(token), line: token.SourceInfo.LineNumber.ToString());
            }

            return new DfmExtractCodeResult
            {
                IsSuccessful = true,
                ErrorMessage = token.PathQueryOption.ErrorMessage,
                CodeLines = Dedent(includedLines, token.PathQueryOption.DedentLength)
            };
        }

        private static string[] Dedent(IEnumerable<string> lines, int? dedentLength = null)
        {
            var length = dedentLength ??
                               (from line in lines
                                where !string.IsNullOrWhiteSpace(line)
                                select (int?)DfmCodeExtractorHelper.GetIndentLength(line)).Min() ?? 0;
            var normalizedLines = (length == 0 ? lines : lines.Select(s => Regex.Replace(s, string.Format(RemoveIndentSpacesRegexString, length), string.Empty))).ToArray();
            return normalizedLines;
        }

        private static string GenerateErrorMessage(DfmFencesToken token)
        {
            return $"{token.PathQueryOption.ErrorMessage} when resolving \"{token.SourceInfo.Markdown.Trim()}\"";
        }
    }
}