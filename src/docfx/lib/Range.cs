// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal readonly struct Range
    {
        /// <summary>
        /// A one based start line value.
        /// </summary>
        public readonly int StartLine;

        /// <summary>
        /// A one based start column value.
        /// </summary>
        public readonly int StartColumn;

        /// <summary>
        /// A one based end line value.
        /// </summary>
        public readonly int EndLine;

        /// <summary>
        /// A one based end column value.
        /// </summary>
        public readonly int EndColumn;

        public Range(int line, int column)
        {
            StartLine = line;
            StartColumn = column;
            EndLine = line;
            EndColumn = column;
        }

        public Range(int startLine, int startColumn, int endLine, int endColumn)
        {
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }
    }
}
