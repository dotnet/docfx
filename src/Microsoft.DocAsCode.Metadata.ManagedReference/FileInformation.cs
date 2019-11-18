// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class FileInformation
    {
        public FileType Type { get; }
        public string NormalizedPath { get; }
        public string RawPath { get; }

        public FileInformation(string raw)
        {
            RawPath = raw;
            NormalizedPath = Normalize(raw);
            Type = GetFileType(raw);
        }

        public override int GetHashCode()
        {
            return NormalizedPath?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(NormalizedPath, (obj as FileInformation)?.NormalizedPath);
        }

        private static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return Path.Combine(EnvironmentContext.BaseDirectory, path).ToNormalizedFullPath();
        }

        private static FileType GetFileType(string filePath)
        {
            var extension = Path.GetExtension(filePath);

            switch (extension.ToLowerInvariant())
            {
                case ".sln":
                    return FileType.Solution;
                case ".csproj":
                case ".fsproj":
                case ".vbproj":
                    return FileType.Project;
                case ".cs":
                    return FileType.CSSourceCode;
                case ".vb":
                    return FileType.VBSourceCode;
                case ".dll":
                case ".exe":
                    return FileType.Assembly;
                default:
                    return FileType.NotSupported;
            }
        }
    }
}
