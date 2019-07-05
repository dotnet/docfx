// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    public class SourceInfo : IEquatable<SourceInfo>, IComparable<SourceInfo>
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

        public int CompareTo(SourceInfo other)
        {
            var result = string.Compare(File, other.File);
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

        public bool Equals(SourceInfo other)
        {
            if (other is null)
                return false;

            return PathUtility.PathComparer.Equals(File, other.File)
                && Line == other.Line
                && Column == other.Column
                && EndLine == other.EndLine
                && EndColumn == other.EndColumn;
        }

        public override bool Equals(object obj)
            => Equals(obj as SourceInfo);

        public override int GetHashCode()
            => HashCode.Combine(PathUtility.PathComparer.GetHashCode(File), Line, Column, EndLine, EndColumn);

        public static bool operator ==(SourceInfo obj1, SourceInfo obj2)
            => Equals(obj1, obj2);

        public static bool operator !=(SourceInfo obj1, SourceInfo obj2)
            => !Equals(obj1, obj2);
    }
}
