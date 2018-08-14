// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class DocumentIdConfig
    {
        /// <summary>
        /// The source base folder path, used by docs.com, for backward compatibility
        /// </summary>
        public string SourceBasePath = ".";

        /// <summary>
        /// The site base path, used by docs.com, for backward compatibility
        /// </summary>
        public string SiteBasePath = ".";

        /// <summary>
        /// The mappings between depot and files/directory
        /// Used for backward compatibility
        /// </summary>
        public Dictionary<string, string> DepotMappings = new Dictionary<string, string>();

        /// <summary>
        /// The mappings between directory and files/directory
        /// Used for backward compatibility
        /// </summary>
        public Dictionary<string, string> DirectoryMappings = new Dictionary<string, string>();

        public (string depotName, string pathRelativeToSourceBasePath) GetMapping(string normalizedFilePathToSourceBasePath)
        {
            var (depotName, _) = GetReversedMapping(DepotMappings, normalizedFilePathToSourceBasePath);
            var (mappedDir, matchedDir) = GetReversedMapping(DirectoryMappings, normalizedFilePathToSourceBasePath);

            var mappedPathRelativeToSourceBasePath = !string.IsNullOrEmpty(matchedDir)
                ? PathUtility.NormalizeFile(Path.Combine(mappedDir, Path.GetRelativePath(matchedDir, normalizedFilePathToSourceBasePath)))
                : normalizedFilePathToSourceBasePath;

            return (depotName, mappedPathRelativeToSourceBasePath);
        }

        private static (string value, string matchedDirectory) GetReversedMapping(Dictionary<string, string> mappings, string normalizedFilePathToSourceBasePath)
        {
            foreach (var (path, value) in mappings)
            {
                var normalizedPath = path.EndsWith("/") || path.EndsWith("\\") ? PathUtility.NormalizeFolder(path) : PathUtility.NormalizeFile(path);
                var (match, isFileMatch, _) = PathUtility.Match(normalizedFilePathToSourceBasePath, normalizedPath);
                if (match)
                {
                    if (!isFileMatch)
                    {
                        return (value, normalizedPath);
                    }

                    var lastSlashIndex = normalizedFilePathToSourceBasePath.LastIndexOf("/");
                    var matchedDirectory = lastSlashIndex > 0 ? normalizedFilePathToSourceBasePath.Substring(0, lastSlashIndex) : string.Empty;
                    return (value, matchedDirectory);
                }
            }

            return (string.Empty, string.Empty);
        }
    }
}
