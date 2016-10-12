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

            var fencesCode = File.ReadAllLines(fencesPath);
            if (token.PathQueryOption == null)
            {
                // Add the full file when no query option is given
                return new DfmExtractCodeResult { IsSuccessful = true, FencesCodeLines = fencesCode };
            }

            if (!token.PathQueryOption.ValidateAndPrepare(fencesCode, token))
            {
                Logger.LogError(token.PathQueryOption.ErrorMessage);
                return new DfmExtractCodeResult { IsSuccessful = false, ErrorMessage = token.PathQueryOption.ErrorMessage, FencesCodeLines = fencesCode };
            }

            var includedLines = new List<string>();
            foreach (var line in token.PathQueryOption.GetQueryLines(fencesCode))
            {
                includedLines.Add(line);
            }

            if (!token.PathQueryOption.ValidateHighlightLinesAndDedentLength(includedLines.Count))
            {
                Logger.LogWarning(token.PathQueryOption.ErrorMessage);
            }

            var dedentLength = token.PathQueryOption.DedentLength ??
                               (from line in includedLines
                                where !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)
                                select (int?)DfmCodeExtractorHelper.GetIndentLength(line)).Min() ?? 0;

            return new DfmExtractCodeResult
            {
                IsSuccessful = true,
                ErrorMessage = token.PathQueryOption.ErrorMessage,
                FencesCodeLines = (dedentLength == 0 ? includedLines : includedLines.Select(s => Regex.Replace(s, string.Format(RemoveIndentSpacesRegexString, dedentLength), string.Empty))).ToArray()
            };
        }
    }
}