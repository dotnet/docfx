// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Application level input abstraction
    /// </summary>
    internal class Input
    {
        private static readonly EnumerationOptions s_enumerationOptions = new EnumerationOptions { RecurseSubdirectories = true };

        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly PackageResolver _packageResolver;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _jsonTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _yamlTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<PathString, byte[]?> _gitBlobCache = new ConcurrentDictionary<PathString, byte[]?>();
        private readonly ConcurrentDictionary<FilePath, JToken> _generatedContents = new ConcurrentDictionary<FilePath, JToken>();
        private readonly PathString? _alternativeFallbackFolder;

        public Input(BuildOptions buildOptions, Config config, PackageResolver packageResolver, RepositoryProvider repositoryProvider)
        {
            _config = config;
            _buildOptions = buildOptions;
            _packageResolver = packageResolver;
            _repositoryProvider = repositoryProvider;
            if (!Directory.Exists(_alternativeFallbackFolder = _buildOptions.DocsetPath.Concat(new PathString(".fallback"))))
            {
                _alternativeFallbackFolder = null;
            }
        }

        /// <summary>
        /// Check if the specified file path exist.
        /// </summary>
        public bool Exists(FilePath file)
        {
            if (file.Origin == FileOrigin.Generated)
            {
                return _generatedContents.ContainsKey(file);
            }

            var fullPath = GetFullPath(file);

            return file.IsGitCommit ? ReadBytesFromGit(fullPath) != null : File.Exists(fullPath);
        }

        public PathString GetFullPath(FilePath file)
        {
            switch (file.Origin)
            {
                case FileOrigin.Main:
                case FileOrigin.Generated:
                case FileOrigin.External:
                    return _buildOptions.DocsetPath.Concat(file.Path);

                case FileOrigin.Dependency:
                    var package = _config.Dependencies[file.DependencyName];
                    var packagePath = _packageResolver.ResolvePackage(package, package.PackageFetchOptions);
                    var pathToPackage = Path.GetRelativePath(file.DependencyName, file.Path);
                    Debug.Assert(!pathToPackage.StartsWith('.'));
                    return new PathString(Path.Combine(packagePath, pathToPackage));

                case FileOrigin.Fallback when _buildOptions.FallbackDocsetPath != null:
                    if (_alternativeFallbackFolder != null)
                    {
                        var pathFromFallbackFolder = _alternativeFallbackFolder.Value.Concat(file.Path);
                        if (File.Exists(pathFromFallbackFolder))
                        {
                            return pathFromFallbackFolder;
                        }
                    }
                    return _buildOptions.FallbackDocsetPath.Value.Concat(file.Path);

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
            if (!file.IsGitCommit && file.Origin != FileOrigin.Generated)
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
            if (file.Origin == FileOrigin.Generated)
            {
                return (new List<Error>(), _generatedContents[file]);
            }

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
            if (file.Origin == FileOrigin.Generated)
            {
                return (new List<Error>(), _generatedContents[file]);
            }

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
            if (file.Origin == FileOrigin.Generated)
            {
                throw new NotSupportedException();
            }

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
                case FileOrigin.Main:
                    return GetFiles(_buildOptions.DocsetPath).Select(file => FilePath.Content(file)).ToArray();

                case FileOrigin.Fallback when _buildOptions.FallbackDocsetPath != null:
                    var files = _alternativeFallbackFolder != null
                        ? GetFiles(_alternativeFallbackFolder).Select(f => FilePath.Fallback(f))
                        : Array.Empty<FilePath>();
                    return files.Concat(GetFiles(_buildOptions.FallbackDocsetPath).Select(f => FilePath.Fallback(f))).ToArray();

                case FileOrigin.Dependency when dependencyName != null:
                    var package = _config.Dependencies[dependencyName.Value];
                    var packagePath = _packageResolver.ResolvePackage(package, package.PackageFetchOptions);

                    return (
                        from file in GetFiles(packagePath)
                        let path = dependencyName.Value.Concat(file)
                        select FilePath.Dependency(path, dependencyName.Value)).ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
            }
        }

        public void AddGeneratedContent(FilePath file, JToken content)
        {
            Debug.Assert(file.Origin == FileOrigin.Generated);

            _generatedContents.TryAdd(file, content);
        }

        private IEnumerable<PathString> GetFiles(string directory)
        {
            return new FileSystemEnumerable<PathString>(directory, ToPathString, s_enumerationOptions)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory && entry.FileName[0] != '.',
                ShouldRecursePredicate =
                 (ref FileSystemEntry entry) => entry.FileName[0] != '.' && !entry.FileName.Equals("_site", StringComparison.OrdinalIgnoreCase),
            };

            static PathString ToPathString(ref FileSystemEntry entry)
            {
                Debug.Assert(!entry.IsDirectory);

                var path = entry.RootDirectory.Length == entry.Directory.Length
                    ? entry.FileName.ToString()
                    : string.Concat(entry.Directory.Slice(entry.RootDirectory.Length + 1), "/", entry.FileName);

                return PathString.DangerousCreate(path);
            }
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
