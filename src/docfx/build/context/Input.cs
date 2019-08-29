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
            switch (file.Origin)
            {
                case FileOrigin.Default when file.Commit is null:
                    return File.OpenRead(Path.Combine(_docsetPath, file.Path));

                case FileOrigin.Dependency when file.Commit is null:
                    var (dependencyPath, _) = _restoreMap.GetGitRestorePath(_config.Dependencies[file.DependencyName], _docsetPath);
                    return File.OpenRead(Path.Combine(dependencyPath, file.Path));

                case FileOrigin.Fallback when file.Commit is null && _fallbackPath != null:
                    return File.OpenRead(Path.Combine(_fallbackPath, file.Path));

                case FileOrigin.Fallback when _fallbackPath != null:
                    var bytes = _gitBlobCache.GetOrAdd(file, aFile => GitUtility.ReadBytes(_fallbackPath, aFile.Path, aFile.Commit))
                        ?? throw new InvalidOperationException($"Error reading '{file}'");
                    return new MemoryStream(bytes, writable: false);

                case FileOrigin.Template when file.Commit is null:
                    var (templatePath, _) = _restoreMap.GetGitRestorePath(_config.Template, _docsetPath);
                    return File.OpenRead(Path.Combine(templatePath, file.Path));

                default:
                    throw new NotSupportedException($"Cannot read file path '{file}'");
            }
        }
    }
}
