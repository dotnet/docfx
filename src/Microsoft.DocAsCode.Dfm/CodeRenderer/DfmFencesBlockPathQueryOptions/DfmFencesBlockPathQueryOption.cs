// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public abstract class DfmFencesBlockPathQueryOption : IDfmFencesBlockPathQueryOption
    {
        public string HighlightLines { get; set; }

        public int? DedentLength { get; set; }

        public string ErrorMessage { get; protected set; }

        public abstract IEnumerable<string> GetQueryLines(string[] lines, DfmFencesToken token);

        public IEnumerable<string> GetQueryLines(string[] lines)
        {
            throw new NotSupportedException();
        }

        public bool ValidateAndPrepare(string[] lines, DfmFencesToken token)
        {
            throw new NotSupportedException();
        }

        bool IDfmFencesBlockPathQueryOption.ValidateHighlightLinesAndDedentLength(int totalLines)
        {
            throw new NotSupportedException();
        }

        protected IEnumerable<string> ProcessIncludedLines(List<string> lines, DfmFencesToken token)
        {
            var message = ValidateHighlightLinesAndDedentLength(lines.Count);
            if (!string.IsNullOrEmpty(message))
            {
                Logger.LogWarning(
                    DfmCodeExtractor.GenerateErrorMessage(token, message),
                    line: token.SourceInfo.LineNumber.ToString());
                ErrorMessage = message;

            }
            return DfmCodeExtractor.Dedent(lines, DedentLength);
        }
            protected string ValidateHighlightLinesAndDedentLength(int totalLines)
        {
            var warningMessages = new List<string>();

            if (!string.IsNullOrEmpty(HighlightLines))
            {
                var ranges = HighlightLines.Split(',');
                int? startLine, endLine;
                int tempStartLine, tempEndLine;
                foreach (var range in ranges)
                {
                    var match = DfmFencesRule._dfmFencesRangeQueryStringRegex.Match(range);
                    if (match.Success)
                    {
                        // consider region as `{startlinenumber}-{endlinenumber}`, in which {endlinenumber} is optional
                        startLine = int.TryParse(match.Groups["start"].Value, out tempStartLine) ? tempStartLine : (int?)null;
                        endLine = int.TryParse(match.Groups["start"].Value, out tempEndLine) ? tempEndLine : (int?)null;
                    }
                    else
                    {
                        // consider region as a sigine line number
                        if (int.TryParse(range, out tempStartLine))
                        {
                            startLine = tempStartLine;
                            endLine = startLine;
                        }
                        else
                        {
                            warningMessages.Add($"Illegal range `{range}` in query parameter `highlight`.");
                            continue;
                        }
                    }
                    if (!CheckLineRange(totalLines, startLine, endLine, false))
                    {
                        warningMessages.Add(ErrorMessage + " in query parameter `highlight`.");
                    }
                }
            }

            if (DedentLength != null && DedentLength < 0)
            {
                warningMessages.Add($"Dedent length {DedentLength} should be positive. Auto-dedent will be applied.");
                DedentLength = null;
            }

            return string.Join(" ", warningMessages);
        }

        protected bool CheckLineRange(int totalLines, int? startLine, int? endLine, bool needThrow = true)
        {
            if (startLine == null && endLine == null)
            {
                if (needThrow)
                {
                    throw new DfmCodeExtractorException("Neither start line nor end line is specified correctly");
                }
                return false;
            }

            if (startLine <= 0 || endLine <= 0)
            {
                if (needThrow)
                {
                    throw new DfmCodeExtractorException("Start/End line should be larger than zero");
                }
                return false;
            }

            if (startLine > endLine)
            {
                if (needThrow)
                {
                    throw new DfmCodeExtractorException($"Start line {startLine} shouldn't be larger than end line {endLine}");
                }
                return false;
            }

            if (startLine > totalLines)
            {
                if (needThrow)
                {
                    throw new DfmCodeExtractorException($"Start line {startLine} exceeds total lines {totalLines}");
                }
                return false;
            }

            return true;
        }

    }
}
