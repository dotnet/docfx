// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal record SourceInfo(FilePath File) : IComparable<SourceInfo>
    {
        /// <summary>
        /// A one based start line value, or zero if there is no line info.
        /// </summary>
        public int Line { get; init; }

        /// <summary>
        /// A one based start column value, or zero if there is no line info.
        /// </summary>
        public int Column { get; init; }

        /// <summary>
        /// A one based end line value, or zero if there is no line info.
        /// </summary>
        public int EndLine { get; init; }

        /// <summary>
        /// A one based end column value, or zero if there is no line info.
        /// </summary>
        public int EndColumn { get; init; }

        /// <summary>
        /// A special storage for source info of the JObject property key
        /// if this is a JObject property value.
        /// </summary>
        public SourceInfo? KeySourceInfo { get; init; }

        public SourceInfo(FilePath file, int startLine, int startColumn, int? endLine = null, int? endColumn = null)
            : this(file)
        {
            File = file;
            Line = startLine;
            Column = startColumn;
            EndLine = endLine ?? startLine;
            EndColumn = endColumn ?? startColumn;
        }

        public SourceInfo WithOffset(SourceInfo? offset)
        {
            if (offset is null)
            {
                return this;
            }

            if (offset.File != File)
            {
                return offset;
            }

            return WithOffset(offset.Line, offset.Column, offset.EndLine, offset.EndColumn);
        }

        public SourceInfo WithOffset(int line, int column)
        {
            var start = OffSet(Line, Column, line, column);

            return this with { Line = start.line, Column = start.column, EndLine = start.line, EndColumn = start.column };
        }

        public SourceInfo WithOffset(int line, int column, int endLine, int endColumn)
        {
            var start = OffSet(Line, Column, line, column);
            var end = OffSet(Line, Column, endLine, endColumn);

            return this with { Line = start.line, Column = start.column, EndLine = end.line, EndColumn = end.column };
        }

        public override string ToString()
        {
            if (ApexValidationExtension.ForceSourceInfoToStringFilePathOnly)
            {
                return File.ToString();
            }
            return Line <= 1 && Column <= 1 ? File.ToString() : $"{File}({Line},{Column})";
        }

        public int CompareTo(SourceInfo? other)
        {
            if (other is null)
            {
                return 1;
            }

            var result = File.CompareTo(other.File);
            if (result == 0)
            {
                result = Line - other.Line;
            }

            if (result == 0)
            {
                result = Column - other.Column;
            }

            if (result == 0)
            {
                result = EndLine - other.EndLine;
            }

            if (result == 0)
            {
                result = EndColumn - other.EndColumn;
            }

            return result;
        }

        private static (int line, int column) OffSet(int line1, int column1, int line2, int column2)
        {
            line1 = line1 <= 0 ? 1 : line1;
            column1 = column1 <= 0 ? 1 : column1;
            line2 = line2 <= 0 ? 1 : line2;
            column2 = column2 <= 0 ? 1 : column2;

            return line2 == 1 ? (line1, column1 + column2 - 1) : (line1 + line2 - 1, column2);
        }
    }
}
