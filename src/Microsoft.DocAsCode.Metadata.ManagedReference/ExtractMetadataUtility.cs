// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class ExtractMetadataUtility
    {
        public static IEnumerable<FileInfo> GetInputs(IEnumerable<string> inputs)
        {
            foreach (var item in inputs)
            {
                yield return new FileInfo
                {
                    RawFilePath = item,
                    FilePath = Normalize(item),
                    Type = GetFileType(item)
                };
            }
        }

        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, path)).ToNormalizedPath();
        }

        public static FileType GetFileType(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            var fileName = Path.GetFileName(filePath);
            if (fileName.Equals("project.json", StringComparison.OrdinalIgnoreCase))
            {
                return FileType.ProjectJsonProject;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".sln":
                    return FileType.Solution;
                case ".csproj":
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
