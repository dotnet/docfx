// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class DocumentIdConfig
    {
        private readonly Lazy<Dictionary<string, string>> _reversedDepotMappings;
        private readonly Lazy<Dictionary<string, string>> _reversedDirectoryMappings;

        /// <summary>
        /// The source base folder path, used by docs.com, for backward compatibility
        /// </summary>
        public string SourceBasePath = string.Empty;

        /// <summary>
        /// The site base path, used by docs.com, for backward compatibility
        /// </summary>
        public string SiteBasePath = string.Empty;

        /// <summary>
        /// The mappings between depot and files/directory
        /// Used for backward compatibility
        /// </summary>
        public Dictionary<string, List<string>> DepotMappings = new Dictionary<string, List<string>>();

        /// <summary>
        /// The mappings between directory and files/directory
        /// Used for backward compatibility
        /// </summary>
        public Dictionary<string, List<string>> DirectoryMappings = new Dictionary<string, List<string>>();

        public DocumentIdConfig()
        {
            _reversedDepotMappings = new Lazy<Dictionary<string, string>>(() => ReverseAndNormalizeMappings(DepotMappings));
            _reversedDirectoryMappings = new Lazy<Dictionary<string, string>>(() => ReverseAndNormalizeMappings(DirectoryMappings));
        }

        public (string depotName, string pathRelativeToSourceBasePath) GetMapping(string normalizedFilePathToSourceBasePath)
        {
            var (depotName, _) = GetReversedMapping(_reversedDepotMappings.Value, normalizedFilePathToSourceBasePath);
            var (mappedDir, matchedDir) = GetReversedMapping(_reversedDirectoryMappings.Value, normalizedFilePathToSourceBasePath);

            var mappedPathRelativeToSourceBasePath = !string.IsNullOrEmpty(matchedDir)
                ? PathUtility.NormalizeFile(Path.Combine(mappedDir, Path.GetRelativePath(matchedDir, normalizedFilePathToSourceBasePath)))
                : normalizedFilePathToSourceBasePath;

            return (depotName, mappedPathRelativeToSourceBasePath);
        }

        private static (string value, string matchedDirectory) GetReversedMapping(Dictionary<string, string> mappings, string normalizedFilePathToSourceBasePath)
        {
            foreach (var (path, value) in mappings)
            {
                if (string.Equals(path, normalizedFilePathToSourceBasePath, PathUtility.PathComparison))
                {
                    var lastSlashIndex = normalizedFilePathToSourceBasePath.LastIndexOf("/");
                    var matchedDirectory = lastSlashIndex > 0 ? normalizedFilePathToSourceBasePath.Substring(0, lastSlashIndex) : string.Empty;
                    return (value, matchedDirectory);
                }

                if (string.IsNullOrEmpty(path) || (path.EndsWith('/') && normalizedFilePathToSourceBasePath.StartsWith(path)))
                {
                    return (value, path);
                }
            }

            return (string.Empty, string.Empty);
        }

        private static Dictionary<string, string> ReverseAndNormalizeMappings(Dictionary<string, List<string>> mappings)
        {
            var reversedMapping = new Dictionary<string, string>();

            foreach (var (key, value) in mappings)
            {
                foreach (var path in value)
                {
                    var normalizedPath = path.EndsWith("/") || path.EndsWith("\\") ? PathUtility.NormalizeFolder(path) : PathUtility.NormalizeFile(path);
                    reversedMapping[normalizedPath] = key;
                }
            }

            return reversedMapping;
        }
    }
}
