// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Input
    {
        private readonly string _docsetPath;
        private readonly string _fallbackPath;
        private readonly Config _config;
        private readonly RestoreGitMap _restoreMap;
        private readonly ConcurrentDictionary<FilePath, byte[]> _gitBlobCache = new ConcurrentDictionary<FilePath, byte[]>();

        public Input(string docsetPath, string fallbackPath, Config config, RestoreGitMap restoreMap)
        {
            _config = config;
            _restoreMap = restoreMap;
            _docsetPath = Path.GetFullPath(docsetPath);
            _fallbackPath = fallbackPath is null ? null : Path.GetFullPath(fallbackPath);
        }

        public bool Exists(FilePath file)
        {
            var (basePath, path, commit) = ResolveFilePath(file);

            if (basePath is null)
            {
                return false;
            }

            if (commit is null)
            {
                return File.Exists(Path.Combine(basePath, path));
            }

            throw new NotSupportedException("Checking if a file exists in a git repo");
        }

        public bool TryGetPhysicalPath(FilePath file, out string physicalPath)
        {
            var (basePath, path, commit) = ResolveFilePath(file);

            if (basePath != null && commit is null)
            {
                var fullPath = Path.Combine(basePath, path);
                if (File.Exists(fullPath))
                {
                    physicalPath = fullPath;
                    return true;
                }
            }

            physicalPath = null;
            return false;
        }

        public string ReadText(FilePath file)
        {
            using (var stream = ReadStream(file))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public Stream ReadStream(FilePath file)
        {
            var (basePath, path, commit) = ResolveFilePath(file);

            if (basePath is null)
            {
                throw new NotSupportedException($"Cannot read file path '{file}'");
            }

            if (commit is null)
            {
                return File.OpenRead(Path.Combine(basePath, path));
            }

            var bytes = _gitBlobCache.GetOrAdd(file, aFile => GitUtility.ReadBytes(_fallbackPath, aFile.Path, aFile.Commit))
                ?? throw new InvalidOperationException($"Error reading '{file}'");

            return new MemoryStream(bytes, writable: false);
        }

        private (string basePath, string path, string commit) ResolveFilePath(FilePath file)
        {
            switch (file.Origin)
            {
                case FileOrigin.Default when file.Commit is null:
                    return (_docsetPath, file.Path, null);

                case FileOrigin.Dependency when file.Commit is null:
                    var (dependencyPath, _) = _restoreMap.GetGitRestorePath(_config.Dependencies[file.DependencyName], _docsetPath);
                    return (dependencyPath, file.Path, null);

                case FileOrigin.Fallback when file.Commit is null && _fallbackPath != null:
                    return (_fallbackPath, file.Path, null);

                case FileOrigin.Fallback when _fallbackPath != null:
                    return (_fallbackPath, file.Path, file.Commit);

                case FileOrigin.Template when file.Commit is null:
                    var (templatePath, _) = _restoreMap.GetGitRestorePath(_config.Template, _docsetPath);
                    return (templatePath, file.Path, null);

                default:
                    return default;
            }
        }
    }
}
