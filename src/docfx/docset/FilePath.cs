// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Docs.Build
{
    internal class FilePath : IEquatable<FilePath>
    {
        public string Path { get; }

        public FileFrom From { get; }

        public override string ToString() => Path;

        public bool Equals(FilePath other)
        {
            return other?.Path == Path && other?.From == From;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(FilePath filePath) => filePath?.Path;

        public FilePath(Document file)
            : this(file?.FilePath, file?.Docset)
        {
        }

        public FilePath(string path, Docset docset)
            : this(path, docset != null && docset.IsFallback() ? FileFrom.Fallback : FileFrom.Current)
        {
        }

        public FilePath(string path, FileFrom from = FileFrom.Current)
        {
            Path = path;
            From = from;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FilePath);
        }
    }
}
