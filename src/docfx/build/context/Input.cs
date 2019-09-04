// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Application level input abstraction
    /// </summary>
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

        /// <summary>
        /// Check if the specified file path exist.
        /// </summary>
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

            throw new NotSupportedException($"{nameof(Exists)}: {file}");
        }

        /// <summary>
        /// Try get the absolute path of the specified file if it exists physically on disk.
        /// Some file path like content from a bare git repo does not exist physically
        /// on disk but we can still read its content.
        /// </summary>
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

        /// <summary>
        /// Reads the specified file as a string.
        /// </summary>
        public string ReadString(FilePath file)
        {
            using (var reader = ReadText(file))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Open the specified file and read it as text.
        /// </summary>
        public TextReader ReadText(FilePath file)
        {
            var (basePath, path, commit) = ResolveFilePath(file);

            if (basePath is null)
            {
                throw new NotSupportedException($"{nameof(ReadText)}: {file}");
            }

            if (commit is null)
            {
                return File.OpenText(Path.Combine(basePath, path));
            }

            var bytes = _gitBlobCache.GetOrAdd(file, aFile => GitUtility.ReadBytes(_fallbackPath, aFile.Path, aFile.Commit))
                ?? throw new InvalidOperationException($"Error reading '{file}'");

            return new StreamReader(new MemoryStream(bytes, writable: false));
        }

        /// <summary>
        /// Reads all the file path.
        /// </summary>
        public FilePath[] ReadFilesRecursive(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Default:
                    return Directory
                        .GetFiles(_docsetPath, "*", SearchOption.AllDirectories)
                        .Select(path => new FilePath(
                            Path.GetRelativePath(_docsetPath, path).Replace('\\', '/'), FileOrigin.Default))
                        .ToArray();

                case FileOrigin.Fallback:
                    return Directory
                        .GetFiles(_fallbackPath, "*", SearchOption.AllDirectories)
                        .Select(path => new FilePath(
                            Path.GetRelativePath(_fallbackPath, path).Replace('\\', '/'), FileOrigin.Fallback))
                        .ToArray();

                case FileOrigin.Dependency:
                    var (dependencyPath, _) = _restoreMap.GetGitRestorePath(_config.Dependencies[dependencyName], _docsetPath);
                    return Directory
                        .GetFiles(dependencyPath, "*", SearchOption.AllDirectories)
                        .Select(path => new FilePath(
                            Path.GetRelativePath(dependencyPath, path).Replace('\\', '/'), FileOrigin.Fallback))
                        .ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ReadFilesRecursive)}: {origin}");
            }
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
