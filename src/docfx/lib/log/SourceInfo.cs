// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class SourceInfo : IEquatable<SourceInfo>, IComparable<SourceInfo>
    {
        /// <summary>
        /// Path to the source file.
        /// </summary>
        public FilePath File { get; }

        /// <summary>
        /// A one based start line value, or zero if there is no line info.
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// A one based start column value, or zero if there is no line info.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// A one based end line value, or zero if there is no line info.
        /// </summary>
        public int EndLine { get; }

        /// <summary>
        /// A one based end column value, or zero if there is no line info.
        /// </summary>
        public int EndColumn { get; }

        /// <summary>
        /// A special storage for source info of the JObject property key
        /// if this is a JObject property value.
        /// </summary>
        public SourceInfo? KeySourceInfo { get; }

        public SourceInfo(FilePath file)
            : this(file, 0, 0, 0, 0)
        { }

        public SourceInfo(FilePath file, int line, int column, SourceInfo? keySourceInfo = null)
            : this(file, line, column, line, column, keySourceInfo)
        { }

        public SourceInfo(
            FilePath file, int startLine, int startColumn, int endLine, int endColumn, SourceInfo? keySourceInfo = null)
        {
            File = file;
            Line = startLine;
            Column = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            KeySourceInfo = keySourceInfo;
        }

        public SourceInfo WithFile(FilePath file)
        {
            return file == File ? this : new SourceInfo(file, Line, Column, EndLine, EndColumn, KeySourceInfo);
        }

        public SourceInfo WithKeySourceInfo(SourceInfo? keySourceInfo)
        {
            return new SourceInfo(File, Line, Column, EndLine, EndColumn, keySourceInfo);
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

            return new SourceInfo(File, start.line, start.column, start.line, start.column);
        }

        public SourceInfo WithOffset(int line, int column, int endLine, int endColumn)
        {
            var start = OffSet(Line, Column, line, column);
            var end = OffSet(Line, Column, endLine, endColumn);

            return new SourceInfo(File, start.line, start.column, end.line, end.column);
        }

        public static bool operator ==(SourceInfo? a, SourceInfo? b) => Equals(a, b);

        public static bool operator !=(SourceInfo? a, SourceInfo? b) => !Equals(a, b);

        public override bool Equals(object? obj)
        {
            return Equals(obj as SourceInfo);
        }

        public bool Equals(SourceInfo? other)
        {
            return other != null &&
                   File.Equals(other.File) &&
                   Line.Equals(other.Line) &&
                   Column.Equals(other.Column) &&
                   EndLine.Equals(other.EndLine) &&
                   EndColumn.Equals(other.EndColumn);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Line, Column, EndLine, EndColumn);
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
