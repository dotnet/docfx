// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public class SourceInfo
    {
        public static readonly SourceInfo Empty = new SourceInfo(null, 0, 0);

        /// <summary>
        /// Path to the source file.
        /// </summary>
        public readonly string File;

        /// <summary>
        /// A one based start line value.
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// A one based start column value.
        /// </summary>
        public readonly int Column;

        /// <summary>
        /// A one based end line value.
        /// </summary>
        public readonly int EndLine;

        /// <summary>
        /// A one based end column value.
        /// </summary>
        public readonly int EndColumn;

        public SourceInfo(string file, int line, int column)
        {
            File = file;
            Line = line;
            Column = column;
            EndLine = line;
            EndColumn = column;
        }

        public SourceInfo(string file, int startLine, int startColumn, int endLine, int endColumn)
        {
            File = file;
            Line = startLine;
            Column = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        internal virtual object GetValue() => null;
    }
}
