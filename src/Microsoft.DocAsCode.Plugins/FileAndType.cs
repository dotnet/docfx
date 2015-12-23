// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.IO;

    public sealed class FileAndType
        : IEquatable<FileAndType>
    {
        public FileAndType(string baseDir, string file, DocumentType type, Func<string, string> pathRewriter)
        {
            if (baseDir == null)
            {
                throw new ArgumentNullException(nameof(baseDir));
            }
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            if (!Path.IsPathRooted(baseDir))
            {
                throw new ArgumentException("Base directory must be rooted.", nameof(baseDir));
            }
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new ArgumentException("File cannot be empty or whitespace.", nameof(file));
            }
            if (Path.IsPathRooted(file))
            {
                throw new ArgumentException("File cannot be rooted.", nameof(file));
            }

            BaseDir = baseDir;
            File = file.Replace('\\', '/');
            Type = type;
            PathRewriter = pathRewriter;
        }

        public string BaseDir { get; }

        public string File { get; }

        public DocumentType Type { get; }

        public Func<string, string> PathRewriter { get; }

        public FileAndType ChangeType(DocumentType type)
        {
            return new FileAndType(BaseDir, File, type, PathRewriter);
        }

        public bool Equals(FileAndType other)
        {
            if (other == null)
            {
                return false;
            }
            return File == other.File && Type == other.Type && BaseDir == other.BaseDir;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileAndType);
        }

        public override int GetHashCode()
        {
            return File.GetHashCode() + (int)Type ^ BaseDir.GetHashCode();
        }

        public static bool operator ==(FileAndType left, FileAndType right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(left, null))
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(FileAndType left, FileAndType right)
        {
            return !(left == right);
        }
    }
}
