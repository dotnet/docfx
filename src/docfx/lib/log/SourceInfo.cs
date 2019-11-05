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
        public readonly FilePath File;

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

        public SourceInfo(FilePath file, int line, int column)
            : this(file, line, column, line, column)
        { }

        public SourceInfo(FilePath file, int startLine, int startColumn, int endLine, int endColumn)
        {
            File = file;
            Line = startLine;
            Column = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceInfo);
        }

        public bool Equals(SourceInfo other)
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

        public int CompareTo(SourceInfo other)
        {
            if (other is null)
                return 1;

            var result = File.CompareTo(other.File);
            if (result == 0)
                result = Line - other.Line;
            if (result == 0)
                result = Column - other.Column;
            if (result == 0)
                result = EndLine - other.EndLine;
            if (result == 0)
                result = EndColumn - other.EndColumn;

            return result;
        }
    }
}
