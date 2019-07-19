// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Docs.Build
{
    internal class FilePath : IEquatable<FilePath>, IComparable<FilePath>
    {
        /// <summary>
        /// The file relative path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the value to indicate where is this file from
        /// </summary>
        public FileFrom From { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(FilePath filePath) => filePath?.Path;

        public FilePath(string path, Docset docset)
            : this(path, docset != null && docset.IsFallback() ? FileFrom.Fallback : FileFrom.Current)
        {
        }

        public FilePath(string path, FileFrom from = FileFrom.Current)
        {
            Path = path;
            From = from;
        }

        public override string ToString() => Path;

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PathUtility.PathComparer.GetHashCode(Path), From);
        }

        public bool Equals(FilePath other)
        {
            return other?.Path == Path && other?.From == From;
        }

        public int CompareTo(FilePath other)
        {
            var result = PathUtility.PathComparer.Compare(Path, other?.Path);
            if (result == 0)
                result = From.CompareTo(other?.From);

            return result;
        }
    }
}
