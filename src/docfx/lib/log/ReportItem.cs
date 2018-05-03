// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class ReportItem
    {
        public ReportLevel Level { get; }

        public string Message { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }

        public string Code { get; }

        public DateTime ReportTimeUtc { get; }

        public ReportItem(ReportLevel level, string code, string message, string file, int line, int column)
        {
            Debug.Assert(!Regex.IsMatch(code, "\\s"), "Code should not contain spaces!");
            Level = level;
            Message = message;
            Code = code;
            Line = line;
            Column = column;
            ReportTimeUtc = DateTime.UtcNow;
        }
    }
}
