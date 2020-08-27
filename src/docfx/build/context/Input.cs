// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
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
        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly PackageResolver _packageResolver;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly MemoryCache<FilePath, JToken> _jsonTokenCache = new MemoryCache<FilePath, JToken>();
        private readonly MemoryCache<FilePath, JToken> _yamlTokenCache = new MemoryCache<FilePath, JToken>();
        private readonly MemoryCache<PathString, byte[]?> _gitBlobCache = new MemoryCache<PathString, byte[]?>();
        private readonly ConcurrentDictionary<FilePath, (string? yamlMime, JToken generatedContent)> _generatedContents =
            new ConcurrentDictionary<FilePath, (string?, JToken)>();

        private readonly ConcurrentDictionary<FilePath, SourceInfo<string?>> _mimeTypeCache
                   = new ConcurrentDictionary<FilePath, SourceInfo<string?>>();

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
        public JToken ReadJson(ErrorBuilder errors, FilePath file)
        {
            if (file.Origin == FileOrigin.Generated)
            {
                return _generatedContents[file].generatedContent;
            }

            return _jsonTokenCache.GetOrAdd(file, path =>
            {
                using var reader = ReadText(path);
                return JsonUtility.Parse(errors, reader, path);
            });
        }

        /// <summary>
        /// Reads the specified file as YAML.
        /// </summary>
        public JToken ReadYaml(ErrorBuilder errors, FilePath file)
        {
            if (file.Origin == FileOrigin.Generated)
            {
                return _generatedContents[file].generatedContent;
            }

            return _yamlTokenCache.GetOrAdd(file, path =>
            {
                using var reader = ReadText(path);
                return YamlUtility.Parse(errors, reader, path);
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
                    return PathUtility.GetFiles(_buildOptions.DocsetPath).Select(file => FilePath.Content(file)).ToArray();

                case FileOrigin.Fallback when _buildOptions.FallbackDocsetPath != null:
                    var files = _alternativeFallbackFolder != null
                        ? PathUtility.GetFiles(_alternativeFallbackFolder).Select(f => FilePath.Fallback(f))
                        : Array.Empty<FilePath>();
                    return files.Concat(PathUtility.GetFiles(_buildOptions.FallbackDocsetPath).Select(f => FilePath.Fallback(f))).ToArray();

                case FileOrigin.Dependency when dependencyName != null:
                    var package = _config.Dependencies[dependencyName.Value];
                    var packagePath = _packageResolver.ResolvePackage(package, package.PackageFetchOptions);

                    return (
                        from file in PathUtility.GetFiles(packagePath)
                        let path = dependencyName.Value.Concat(file)
                        select FilePath.Dependency(path, dependencyName.Value)).ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
            }
        }

        public void AddGeneratedContent(FilePath file, JToken content, string? yamlMime)
        {
            Debug.Assert(file.Origin == FileOrigin.Generated);

            _generatedContents.TryAdd(file, (yamlMime, content));
        }

        public string? GetYamlMimeFromGenerated(FilePath file)
        {
            Debug.Assert(file.Origin == FileOrigin.Generated);

            return _generatedContents[file].yamlMime;
        }

        public SourceInfo<string?> GetMime(ContentType contentType, FilePath filePath)
        {
            return _mimeTypeCache.GetOrAdd(filePath, path =>
            {
                return contentType == ContentType.Page ? ReadMimeFromFile(path) : default;
            });
        }

        private SourceInfo<string?> ReadMimeFromFile(FilePath filePath)
        {
            switch (filePath.Format)
            {
                // TODO: we could have not depend on this exists check, but currently
                //       LinkResolver works with Document and return a Document for token files,
                //       thus we are forced to get the mime type of a token file here even if it's not useful.
                //
                //       After token resolve does not create Document, this Exists check can be removed.
                case FileFormat.Json:
                    using (var reader = ReadText(filePath))
                    {
                        return JsonUtility.ReadMime(reader, filePath);
                    }
                case FileFormat.Yaml:
                    if (filePath.Origin == FileOrigin.Generated)
                    {
                        var yamlMime = GetYamlMimeFromGenerated(filePath);
                        return new SourceInfo<string?>(yamlMime, new SourceInfo(filePath, 1, 1));
                    }
                    using (var reader = ReadText(filePath))
                    {
                        return new SourceInfo<string?>(YamlUtility.ReadMime(reader), new SourceInfo(filePath, 1, 1));
                    }
                case FileFormat.Markdown:
                    return new SourceInfo<string?>("Conceptual", new SourceInfo(filePath, 1, 1));
                default:
                    throw new NotSupportedException();
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
