// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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

        public ReportItem(ReportLevel level, string message, string code, int line, int column)
        {
            Level = level;
            Message = message;
            Code = code;
            Line = line;
            Column = column;
            ReportTimeUtc = DateTime.UtcNow;
        }
    }
}
