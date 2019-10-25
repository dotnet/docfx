// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly string _docsetPath;
        private readonly Func<Config> _config;
        private readonly RepositoryProvider _repositoryProvider;
        private readonly Lazy<string> _docsetPathToRepository;

        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _jsonTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<FilePath, (List<Error>, JToken)> _yamlTokenCache = new ConcurrentDictionary<FilePath, (List<Error>, JToken)>();
        private readonly ConcurrentDictionary<FilePath, byte[]> _gitBlobCache = new ConcurrentDictionary<FilePath, byte[]>();

        public Input(string docsetPath, Func<Config> config, RepositoryProvider repositoryProvider)
        {
            _docsetPath = docsetPath;
            _config = config;
            _repositoryProvider = repositoryProvider;
            _docsetPathToRepository = new Lazy<string>(GetDocsetPathToRepository);
        }

        /// <summary>
        /// Check if the specified file path exist.
        /// </summary>
        public bool Exists(FilePath file)
        {
            var (packagePath, path, bare) = ResolveFilePath(file);

            switch (packagePath.Type)
            {
                case PackageType.Folder:
                    return File.Exists(Path.Combine(_docsetPath, packagePath.Path, path));

                case PackageType.Git:
                    var repo = _repositoryProvider.GetRepository(packagePath.Url, packagePath.Branch, bare);
                    return _gitBlobCache.GetOrAdd(file, _ => GitUtility.ReadBytes(repo.Path, path, file.Commit ?? repo.Commit)) != null;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Try get the absolute path of the specified file if it exists physically on disk.
        /// Some file path like content from a bare git repo does not exist physically
        /// on disk but we can still read its content.
        /// </summary>
        public bool TryGetPhysicalPath(FilePath file, out string physicalPath)
        {
            var (packagePath, path, bare) = ResolveFilePath(file);

            if (packagePath.Type == PackageType.Folder && file.Commit is null)
            {
                var fullPath = Path.Combine(_docsetPath, packagePath.Path, path);
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
        /// Reads the specified file as JSON.
        /// </summary>
        public (List<Error> errors, JToken token) ReadJson(FilePath file)
        {
            return _jsonTokenCache.GetOrAdd(file, path =>
            {
                using (var reader = ReadText(path))
                {
                    return JsonUtility.Parse(reader, path);
                }
            });
        }

        /// <summary>
        /// Reads the specified file as YAML.
        /// </summary>
        public (List<Error> errors, JToken token) ReadYaml(FilePath file)
        {
            return _yamlTokenCache.GetOrAdd(file, path =>
            {
                using (var reader = ReadText(path))
                {
                    return YamlUtility.Parse(reader, path);
                }
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
            var (packagePath, path, bare) = ResolveFilePath(file);

            switch (packagePath.Type)
            {
                case PackageType.Folder:
                    return File.OpenRead(Path.Combine(_docsetPath, packagePath.Path, path));

                case PackageType.Git:
                    var repo = _repositoryProvider.GetRepository(packagePath.Url, packagePath.Branch, bare);
                    var bytes = _gitBlobCache.GetOrAdd(file, _ => GitUtility.ReadBytes(repo.Path, path, file.Commit ?? repo.Commit))
                            ?? throw new InvalidOperationException($"Error reading '{file}'");
                    return new MemoryStream(bytes, writable: false);

                default:
                    throw new NotSupportedException($"{nameof(ReadStream)}: {packagePath.Type}");
            }
        }

        /// <summary>
        /// List all the file path.
        /// </summary>
        public FilePath[] ListFilesRecursive(FileOrigin origin, string dependencyName = null)
        {
            var (packagePath, bare) = GetPackagePath(origin, dependencyName);
            var files = ListFilesRecursive(packagePath, bare);

            switch (origin)
            {
                case FileOrigin.Default:
                    return files.Select(path => new FilePath(path)).ToArray();

                case FileOrigin.Fallback:
                    var docsetPathRelativeToRepository = _docsetPathToRepository.Value;
                    if (docsetPathRelativeToRepository is null)
                    {
                        return Array.Empty<FilePath>();
                    }

                    return (from path in files
                            let pathToDocset = Path.GetRelativePath(docsetPathRelativeToRepository, path)
                            where !pathToDocset.StartsWith('.') // Exclude files that are outside docset folder
                            select new FilePath(pathToDocset, FileOrigin.Fallback)).ToArray();

                case FileOrigin.Dependency:
                    return files.Select(path => new FilePath(path, dependencyName)).ToArray();

                default:
                    throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
            }
        }

        private IEnumerable<string> ListFilesRecursive(PackagePath packagePath, bool bare)
        {
            switch (packagePath.Type)
            {
                case PackageType.Folder:
                    var basePath = Path.Combine(_docsetPath, packagePath.Path);

                    if (!Directory.Exists(basePath))
                    {
                        throw Errors.DirectoryNotFound(new SourceInfo<string>(packagePath.ToString())).ToException();
                    }

                    return Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
                                    .Select(path => Path.GetRelativePath(basePath, path));

                case PackageType.Git:
                    // todo: get tree list from repository
                    var repository = _repositoryProvider.GetRepository(packagePath.Url, packagePath.Branch, bare);
                    return GitUtility.ListTree(repository.Path, repository.Commit);

                default:
                    return Array.Empty<string>();
            }
        }

        private (PackagePath, bool bare) GetPackagePath(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Default:
                    return (new PackagePath(_docsetPath), bare: false);

                case FileOrigin.Fallback:
                    var repository = _repositoryProvider.GetRepository(FileOrigin.Fallback);
                    return (repository is null ? new PackagePath() : new PackagePath(repository.Remote, repository.Branch), bare: true);

                case FileOrigin.Dependency:
                    return (_config().Dependencies[dependencyName], bare: true);

                case FileOrigin.Template:
                    return (_config().Template, bare: false);

                default:
                    throw new NotSupportedException($"{nameof(GetPackagePath)}: {origin}");
            }
        }

        private (PackagePath, string path, bool bare) ResolveFilePath(FilePath file)
        {
            var (packagePath, bare) = GetPackagePath(file.Origin, file.DependencyName);

            switch (file.Origin)
            {
                case FileOrigin.Default:
                    return (packagePath, file.Path, bare);

                case FileOrigin.Dependency:
                    return (packagePath, file.GetPathToOrigin(), bare);

                case FileOrigin.Fallback:
                    var docsetPathToRepository = _docsetPathToRepository.Value;
                    if (docsetPathToRepository is null)
                    {
                        return (new PackagePath(), default, default);
                    }

                    var path = PathUtility.NormalizeFile(Path.Combine(docsetPathToRepository, file.Path));
                    return (packagePath, path, bare);

                case FileOrigin.Template:
                    return (packagePath, file.Path, bare);

                default:
                    throw new NotSupportedException($"{nameof(ResolveFilePath)}: {file.Origin}");
            }
        }

        private string GetDocsetPathToRepository()
        {
            var repo = _repositoryProvider.GetRepository(FileOrigin.Default);

            return repo is null ? null : Path.GetRelativePath(repo.Path, _docsetPath);
        }
    }
}
