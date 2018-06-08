// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Throw this exception in case of known error that should stop execution of the program.
    /// E.g, loading a malformed configuration.
    /// </summary>
    internal class DocfxException : Exception
    {
        public ReportLevel Level { get; }

        public string Code { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }

        public DocfxException(
            ReportLevel level,
            string code,
            string message,
            string file = null,
            int line = 0,
            int column = 0,
            Exception innerException = null)
            : base(message, innerException)
        {
            Level = level;
            Code = code;
            File = file;
            Line = line;
            Column = column;
        }
    }
}
