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
        private readonly RepositoryProvider _repositoryProvider;
        private readonly ConcurrentDictionary<FilePath, byte[]> _gitBlobCache = new ConcurrentDictionary<FilePath, byte[]>();

        public Input(string docsetPath, RepositoryProvider repositoryProvider)
        {
            _repositoryProvider = repositoryProvider;
            _docsetPath = Path.GetFullPath(docsetPath);
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

            return _gitBlobCache.GetOrAdd(file, _ => GitUtility.ReadBytes(basePath, path, commit)) != null;
        }

        /// <summary>
        /// Get the absolute path of current root
        /// </summary>
        public string GetRootPath(FileOrigin origin, string dependencyName = null)
        {
            var (basePath, _, _) = ResolveFilePath(dependencyName != null ? new FilePath("", dependencyName) : new FilePath("", origin));

            return basePath;
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

            var bytes = _gitBlobCache.GetOrAdd(file, _ => GitUtility.ReadBytes(basePath, path, commit))
                ?? throw new InvalidOperationException($"Error reading '{file}'");

            return new StreamReader(new MemoryStream(bytes, writable: false));
        }

        /// <summary>
        /// List all the file path.
        /// </summary>
        public FilePath[] ListFilesRecursive(FileOrigin origin, string dependencyName = null)
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
                    var fallbackRepository = _repositoryProvider.GetRepository(origin);

                    return Directory
                        .GetFiles(fallbackRepository.Path, "*", SearchOption.AllDirectories)
                        .Select(path => new FilePath(
                            Path.GetRelativePath(fallbackRepository.Path, path).Replace('\\', '/'), FileOrigin.Fallback))
                        .ToArray();

                case FileOrigin.Dependency:
                    var dependencyRepository = _repositoryProvider.GetRepository(origin, dependencyName);

                    // todo: get tree list from repository
                    return GitUtility.ListTree(dependencyRepository.Path, dependencyRepository.Commit)
                        .Select(path => new FilePath(
                            path.Replace('\\', '/'), dependencyName))
                        .ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
            }
        }

        private (string basePath, string path, string commit) ResolveFilePath(FilePath file)
        {
            switch (file.Origin)
            {
                case FileOrigin.Default:
                    return (_docsetPath, file.Path, file.Commit);

                case FileOrigin.Dependency:
                    var dependencyRepository = _repositoryProvider.GetRepository(file.Origin, file.DependencyName);
                    return (dependencyRepository.Path, file.Path, file.Commit ?? dependencyRepository.Commit);

                case FileOrigin.Fallback:
                    var fallbackRepository = _repositoryProvider.GetRepository(file.Origin);
                    return (fallbackRepository.Path, file.Path, file.Commit);

                case FileOrigin.Template:
                    var templateRepository = _repositoryProvider.GetRepository(FileOrigin.Template);
                    return (templateRepository.Path, file.Path, file.Commit);

                default:
                    return default;
            }
        }
    }
}
