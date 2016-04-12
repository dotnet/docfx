// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    public abstract class DfmFencesBlockPathQueryOption : IDfmFencesBlockPathQueryOption
    {
        public string ErrorMessage { get; protected set; }

        public abstract bool ValidateAndPrepare(string[] lines, DfmFencesBlockToken token);

        public abstract IEnumerable<string> GetQueryLines(string[] lines);

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
                ErrorMessage = $"Start line '{startLine}' execeeds total file lines '{totalLines}'";
                return false;
            }

            return true;
        }

    }
}
