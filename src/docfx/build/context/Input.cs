// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

/// <summary>
/// Application level input abstraction
/// </summary>
internal class Input
{
    private readonly Config _config;
    private readonly SourceMap _sourceMap;
    private readonly Package _mainPackage;
    private readonly Package? _fallbackPackage;
    private readonly Package? _alternativeFallbackPackage;
    private readonly PackageResolver _packageResolver;
    private readonly RepositoryProvider _repositoryProvider;

    private readonly MemoryCache<PathString, Watch<byte[]?>> _gitBlobCache = new();
    private readonly ConcurrentDictionary<FilePath, Watch<SourceInfo<string?>>> _mimeTypeCache = new();

    private readonly Scoped<ConcurrentDictionary<FilePath, (string? yamlMime, JToken generatedContent)>> _generatedContents = new();

    public Input(
        BuildOptions buildOptions,
        Config config,
        PackageResolver packageResolver,
        RepositoryProvider repositoryProvider,
        SourceMap sourceMap,
        Package package)
    {
        _config = config;
        _sourceMap = sourceMap;
        _packageResolver = packageResolver;
        _repositoryProvider = repositoryProvider;
        _mainPackage = package;

        if (buildOptions.FallbackDocsetPath != null)
        {
            _fallbackPackage = new LocalPackage(buildOptions.FallbackDocsetPath);
        }

        var alternativeFallbackPath = buildOptions.DocsetPath.Concat(new PathString(".fallback"));
        if (Directory.Exists(alternativeFallbackPath))
        {
            _alternativeFallbackPackage = new LocalPackage(alternativeFallbackPath);
        }
    }

    /// <summary>
    /// Check if the specified file path exist.
    /// </summary>
    public bool Exists(FilePath file)
    {
        if (file.Origin == FileOrigin.Generated)
        {
            return _generatedContents.Value.ContainsKey(file);
        }

        if (file.IsGitCommit)
        {
            return ReadBytesFromGit(file) != null;
        }

        var (package, path) = ResolveFilePath(file);

        return package.Exists(path);
    }

    public FilePath? GetFirstMatchInSplitToc(string pathString)
    {
        var path = new PathString(pathString);
        foreach (var (k, _) in _generatedContents.Value)
        {
            if (k.Path.Value == path)
            {
                return k;
            }
        }

        return null;
    }

    /// <summary>
    /// Try get the absolute path of the specified file if it exists physically on disk.
    /// Some file path like content from a bare git repo does not exist physically
    /// on disk but we can still read its content.
    /// </summary>
    public PathString? TryGetPhysicalPath(FilePath file)
    {
        if (file.IsGitCommit || file.Origin == FileOrigin.Generated)
        {
            return default;
        }

        var (package, path) = ResolveFilePath(file);

        return package.TryGetPhysicalPath(path);
    }

    public PathString? TryGetOriginalPhysicalPath(FilePath file)
    {
        if (file.IsGitCommit || file.Origin == FileOrigin.Generated)
        {
            return default;
        }

        var (package, path) = ResolveFilePath(_sourceMap.GetOriginalFilePath(file) ?? file);

        return package.TryGetPhysicalPath(path);
    }

    public DateTime GetLastWriteTimeUtc(FilePath file)
    {
        if (file.IsGitCommit || file.Origin == FileOrigin.Generated)
        {
            return default;
        }

        var (package, path) = ResolveFilePath(_sourceMap.GetOriginalFilePath(file) ?? file);

        return package.TryGetLastWriteTimeUtc(path) ?? default;
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
            return _generatedContents.Value[file].generatedContent;
        }

        using var reader = ReadText(file);
        return JsonUtility.Parse(errors, reader, file);
    }

    /// <summary>
    /// Reads the specified file as YAML.
    /// </summary>
    public JToken ReadYaml(ErrorBuilder errors, FilePath file)
    {
        if (file.Origin == FileOrigin.Generated)
        {
            return _generatedContents.Value[file].generatedContent;
        }

        using var reader = ReadText(file);
        return YamlUtility.Parse(errors, reader, file);
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

        if (file.IsGitCommit)
        {
            var bytes = ReadBytesFromGit(file) ?? throw new InvalidOperationException($"Error reading '{file}'");

            return new MemoryStream(bytes, writable: false);
        }

        var (package, path) = ResolveFilePath(file);

        return package.ReadStream(path);
    }

    /// <summary>
    /// List all the file path.
    /// </summary>
    public FilePath[] ListFilesRecursive(FileOrigin origin, PathString? dependencyName = null)
    {
        switch (origin)
        {
            case FileOrigin.Main:
                return _mainPackage.GetFiles().Select(FilePath.Content).ToArray();

            case FileOrigin.Fallback:
                var alternativeFiles = _alternativeFallbackPackage?.GetFiles() ?? Array.Empty<PathString>();
                var files = _fallbackPackage?.GetFiles() ?? Array.Empty<PathString>();
                return alternativeFiles.Concat(files).Select(file => FilePath.Fallback(file)).ToArray();

            case FileOrigin.Dependency when dependencyName != null:
                var packagePath = _config.Dependencies[dependencyName.Value];
                var package = _packageResolver.ResolveAsPackage(packagePath, packagePath.PackageFetchOptions);

                return (
                    from file in package.GetFiles()
                    let path = dependencyName.Value.Concat(file)
                    select FilePath.Dependency(path, dependencyName.Value)).ToArray();

            default:
                throw new NotSupportedException($"{nameof(ListFilesRecursive)}: {origin}");
        }
    }

    public void AddGeneratedContent(FilePath file, JToken content, string? yamlMime)
    {
        Debug.Assert(file.Origin == FileOrigin.Generated);

        Watcher.Write(() => _generatedContents.Value.TryAdd(file, (yamlMime, content)));
    }

    public SourceInfo<string?> GetMime(ContentType contentType, FilePath filePath)
    {
        return contentType == ContentType.Page ? _mimeTypeCache.GetOrAdd(filePath, path => new(() => ReadMimeFromFile(path))).Value : default;
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

    private string? GetYamlMimeFromGenerated(FilePath file)
    {
        Debug.Assert(file.Origin == FileOrigin.Generated);

        return _generatedContents.Value[file].yamlMime;
    }

    private (Package, PathString) ResolveFilePath(FilePath file)
    {
        switch (file.Origin)
        {
            case FileOrigin.Main:
            case FileOrigin.External:
                return (_mainPackage, file.Path);

            case FileOrigin.Dependency:
                var packagePath = _config.Dependencies[file.DependencyName];
                var package = _packageResolver.ResolveAsPackage(packagePath, packagePath.PackageFetchOptions);
                var pathToPackage = new PathString(Path.GetRelativePath(file.DependencyName, file.Path));
                Debug.Assert(!pathToPackage.Value.StartsWith('.'));
                return (package, pathToPackage);

            case FileOrigin.Fallback when _fallbackPackage != null:
                if (_alternativeFallbackPackage != null && _alternativeFallbackPackage.Exists(file.Path))
                {
                    return (_alternativeFallbackPackage, file.Path);
                }
                return (_fallbackPackage, file.Path);

            default:
                throw new InvalidOperationException();
        }
    }

    private byte[]? ReadBytesFromGit(FilePath file)
    {
        var (package, path) = ResolveFilePath(file);

        return ReadBytesFromGit(package.GetFullFilePath(path));
    }

    private byte[]? ReadBytesFromGit(PathString? physicalPath)
    {
        if (physicalPath is null)
        {
            return null;
        }

        var (repo, pathToRepo) = _repositoryProvider.GetRepository(physicalPath.Value);
        if (repo is null || pathToRepo is null)
        {
            return null;
        }

        return _gitBlobCache.GetOrAdd(physicalPath.Value, path => new(() =>
        {
            var (repo, _, commits) = _repositoryProvider.GetCommitHistory(path);
            if (repo is null || commits.Length <= 1)
            {
                return null;
            }
            return GitUtility.ReadBytes(repo.Path, pathToRepo, commits[1].Sha);
        })).Value;
    }
}
