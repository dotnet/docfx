// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// A docset is a collection of documents in the folder identified by `docfx.yml/docfx.json`.
    /// </summary>
    internal class Docset : IEquatable<Docset>, IComparable<Docset>
    {
        /// <summary>
        /// Gets the absolute path to folder containing `docfx.yml/docfx.json`, it is not necessarily the path to git repository.
        /// </summary>
        public string DocsetPath { get; }

        public Docset(string docsetPath)
        {
            DocsetPath = PathUtility.NormalizeFolder(Path.GetFullPath(docsetPath));
        }

        public int CompareTo(Docset other)
        {
            return string.CompareOrdinal(DocsetPath, other.DocsetPath);
        }

        public override int GetHashCode()
        {
            return PathUtility.PathComparer.GetHashCode(DocsetPath);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Docset);
        }

        public bool Equals(Docset? other)
        {
            if (other is null)
            {
                return false;
            }

            return PathUtility.PathComparer.Equals(DocsetPath, other.DocsetPath);
        }

        public static bool operator ==(Docset? obj1, Docset? obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(Docset? obj1, Docset? obj2)
        {
            return !Equals(obj1, obj2);
        }
    }
}
