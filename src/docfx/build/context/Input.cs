// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Application level input abstraction
    /// </summary>
    internal class Input
    {
        private readonly PathString _docsetPath;
        private readonly Config _config;
        private readonly PackageResolver _packageResolver;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly LocalizationProvider _localizationProvider;
        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _jsonTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _yamlTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<PathString, byte[]?> _gitBlobCache = new ConcurrentDictionary<PathString, byte[]?>();

        public Input(string docsetPath, Config config, PackageResolver packageResolver, RepositoryProvider repositoryProvider, LocalizationProvider localizationProvider)
        {
            _config = config;
            _packageResolver = packageResolver;
            _repositoryProvider = repositoryProvider;
            _localizationProvider = localizationProvider;
            _docsetPath = new PathString(Path.GetFullPath(docsetPath));
        }

        /// <summary>
        /// Check if the specified file path exist.
        /// </summary>
        public bool Exists(FilePath file)
        {
            var fullPath = GetFullPath(file);

            return file.IsGitCommit ? ReadBytesFromGit(fullPath) != null : File.Exists(fullPath);
        }

        public PathString GetFullPath(FilePath file)
        {
            switch (file.Origin)
            {
                case FileOrigin.Default:
                    return _docsetPath.Concat(file.Path);

                case FileOrigin.Dependency:
                    var package = _config.Dependencies[file.DependencyName];
                    var packagePath = _packageResolver.ResolvePackage(package, package.PackageFetchOptions);
                    var pathToPackage = Path.GetRelativePath(file.DependencyName, file.Path);
                    Debug.Assert(!pathToPackage.StartsWith('.'));
                    return new PathString(Path.Combine(packagePath, pathToPackage));

                case FileOrigin.Fallback when _localizationProvider.FallbackDocsetPath != null:
                    return _localizationProvider.FallbackDocsetPath.Value.Concat(file.Path);

                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Try get the absolute path of the specified file if it exists physically on disk.
        /// Some file path like content from a bare git repo does not exist physically
        /// on disk but we can still read its content.
        /// </summary>
        public bool TryGetPhysicalPath(FilePath file, [NotNullWhen(true)] out string? physicalPath)
        {
            if (!file.IsGitCommit)
            {
                var fullPath = GetFullPath(file);
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
            using var reader = ReadText(file);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads the specified file as JSON.
        /// </summary>
        public (List<Error> errors, JToken token) ReadJson(FilePath file)
        {
            return _jsonTokenCache.GetOrAdd(file, path =>
            {
                using var reader = ReadText(path);
                return JsonUtility.Parse(reader, path);
            });
        }

        /// <summary>
        /// Reads the specified file as YAML.
        /// </summary>
        public (List<Error> errors, JToken token) ReadYaml(FilePath file)
        {
            return _yamlTokenCache.GetOrAdd(file, path =>
            {
                using var reader = ReadText(path);
                return YamlUtility.Parse(reader, path);
            });
        }

        /// <summary>
        /// Open the specified file and read it as text.
        /// </summary>
        public TextReader ReadText(FilePath file)
        {
            return new StreamReader(ReadStream(file));
        }

        public Stream ReadStream(FilePath file)
        {
            var fullPath = GetFullPath(file);
            if (!file.IsGitCommit)
            {
                return File.OpenRead(fullPath);
            }

            var bytes = ReadBytesFromGit(fullPath) ?? throw new InvalidOperationException($"Error reading '{file}'");

            return new MemoryStream(bytes, writable: false);
        }

        /// <summary>
        /// List all the file path.
        /// </summary>
        public FilePath[] ListFilesRecursive(FileOrigin origin, PathString? dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Default:
                    return GetFiles(_docsetPath).Select(file => new FilePath(file)).ToArray();

                case FileOrigin.Fallback when _localizationProvider.FallbackDocsetPath != null:
                    return GetFiles(_localizationProvider.FallbackDocsetPath).Select(file => new FilePath(file, FileOrigin.Fallback)).ToArray();

                case FileOrigin.Dependency when dependencyName != null:
                    var package = _config.Dependencies[dependencyName.Value];
                    var packagePath = _packageResolver.ResolvePackage(package, package.PackageFetchOptions);

                    return (
                        from file in GetFiles(packagePath)
                        let path = dependencyName.Value.Concat(file)
                        select new FilePath(path, dependencyName.Value)).ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
            }
        }

        private IEnumerable<PathString> GetFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return Array.Empty<PathString>();
            }

            return from file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                   where !file.Contains("/.git/") && !file.Contains("\\.git\\")
                   let path = new PathString(Path.GetRelativePath(directory, file))
                   where !path.Value.StartsWith('.')
                   select path;
        }

        private byte[]? ReadBytesFromGit(PathString fullPath)
        {
            var (repo, pathToRepo) = _repositoryProvider.GetRepository(fullPath);
            if (repo is null || pathToRepo is null)
            {
                return null;
            }

            return _gitBlobCache.GetOrAdd(fullPath, path =>
            {
                var (repo, _, commits) = _repositoryProvider.GetCommitHistory(path);
                if (repo is null || commits.Length <= 1)
                {
                    return null;
                }
                return GitUtility.ReadBytes(repo.Path, pathToRepo, commits[1].Sha);
            });
        }
    }
}
