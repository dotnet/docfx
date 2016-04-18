// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public abstract class DfmFencesBlockPathQueryOption : IDfmFencesBlockPathQueryOption
    {
        public string HighlightLines { get; set; }

        public string ErrorMessage { get; protected set; }

        public abstract bool ValidateAndPrepare(string[] lines, DfmFencesBlockToken token);

        public abstract IEnumerable<string> GetQueryLines(string[] lines);

        public bool ValidateHighlightLines(int totalLines)
        {
            if (string.IsNullOrEmpty(HighlightLines)) return true;
            var ranges = HighlightLines.Split(',');
            int? startLine, endLine;
            int tempStartLine, tempEndLine;
            foreach (var range in ranges)
            {
                var match = DfmFencesBlockRule._dfmFencesRangeQueryStringRegex.Match(range);
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
                        ErrorMessage = $"Illegal range {range} in query parameter `highlight`";
                        return false;
                    }
                }
                if (!CheckLineRange(totalLines, startLine, endLine))
                {
                    ErrorMessage = ErrorMessage + " in query parameter `highlight`";
                    return false;
                }
            }
            return true;
        }

        protected bool CheckLineRange(int totalLines, int? startLine, int? endLine)
        {
            if (startLine == null && endLine == null)
            {
                ErrorMessage = "Neither start line nor end line is specified correctly";
                return false;
            }

            if (startLine <= 0 || endLine <= 0)
            {
                ErrorMessage = "Start/End line should be larger than zero";
                return false;
            }

            if (startLine > endLine)
            {
                ErrorMessage = $"Start line {startLine} shouldn't be larger than end line {endLine}";
                return false;
            }

            if (startLine > totalLines)
            {
                ErrorMessage = $"Start line {startLine} execeeds total lines {totalLines}";
                return false;
            }

            return true;
        }

    }
}
