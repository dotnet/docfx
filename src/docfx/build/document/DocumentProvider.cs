// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class DocumentProvider
    {
        private readonly Config _config;
        private readonly Docset _docset;
        private readonly Docset _fallbackDocset;
        private readonly Input _input;
        private readonly BuildScope _buildScope;
        private readonly LocalizationProvider _localization;
        private readonly TemplateEngine _templateEngine;

        private readonly string _depotName;
        private readonly (PathString, DocumentIdConfig)[] _documentIdRules;
        private readonly (PathString src, PathString dest)[] _routes;
        private readonly HashSet<string> _configReferences;
        private readonly IReadOnlyDictionary<string, Docset> _dependencyDocsets;

        private readonly ConcurrentDictionary<FilePath, Document> _documents = new ConcurrentDictionary<FilePath, Document>();

        public DocumentProvider(
            Config config, LocalizationProvider localization, Docset docset, Docset fallbackDocset, BuildScope buildScope, Input input, RepositoryProvider repositoryProvider, TemplateEngine templateEngine)
        {
            _config = config;
            _docset = docset;
            _localization = localization;
            _fallbackDocset = fallbackDocset;
            _buildScope = buildScope;
            _dependencyDocsets = LoadDependencies(repositoryProvider);
            _input = input;
            _templateEngine = templateEngine;

            var documentIdConfig = config.GlobalMetadata.DocumentIdDepotMapping ?? config.DocumentId;

            _depotName = string.IsNullOrEmpty(config.Product) ? config.Name : $"{config.Product}.{config.Name}";
            _configReferences = config.Extend.Concat(config.GetFileReferences()).Select(path => path.Value).ToHashSet(PathUtility.PathComparer);
            _documentIdRules = documentIdConfig.Reverse().Select(item => (item.Key, item.Value)).ToArray();
            _routes = config.Routes.Reverse().Select(item => (item.Key, item.Value)).ToArray();
        }

        public Document GetDocument(FilePath path)
        {
            return _documents.GetOrAdd(path, GetDocumentCore);
        }

        public ContentType GetContentType(string path)
        {
            if (_configReferences.Contains(path))
            {
                return ContentType.Unknown;
            }

            if (_buildScope.IsResource(path))
            {
                return ContentType.Resource;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            if (name.Equals("TOC", PathUtility.PathComparison) || name.Equals("TOC.experimental", PathUtility.PathComparison))
            {
                return ContentType.TableOfContents;
            }
            if (name.Equals("docfx", PathUtility.PathComparison))
            {
                return ContentType.Unknown;
            }
            if (name.Equals("redirections", PathUtility.PathComparison))
            {
                return ContentType.Unknown;
            }

            return ContentType.Page;
        }

        public string GetOutputPath(FilePath path, IReadOnlyList<string> monikers)
        {
            var file = GetDocument(path);

            var outputPath = UrlUtility.Combine(_config.BasePath, MonikerUtility.GetGroup(monikers) ?? "", file.SitePath);

            return _config.Legacy && file.IsPage ? LegacyUtility.ChangeExtension(outputPath, ".raw.page.json") : outputPath;
        }

        public (string documentId, string versionIndependentId) GetDocumentId(FilePath path)
        {
            var file = GetDocument(path);

            var depotName = _depotName;
            var sourcePath = file.FilePath.Path.Value;

            if (TryGetDocumentIdConfig(file.FilePath.Path, out var config, out var remainingPath))
            {
                if (!string.IsNullOrEmpty(config.DepotName))
                {
                    depotName = config.DepotName;
                }

                if (config.FolderRelativePathInDocset != null)
                {
                    sourcePath = remainingPath.IsDefault
                        ? config.FolderRelativePathInDocset + file.FilePath.Path.GetFileName()
                        : config.FolderRelativePathInDocset + remainingPath;
                }
            }

            // if source is redirection or landing page, change it to *.md
            if (file.ContentType == ContentType.Redirection || TemplateEngine.IsLandingData(file.Mime))
            {
                sourcePath = Path.ChangeExtension(sourcePath, ".md");
            }

            // remove file extension from site path
            // site path doesn't contain version info according to the output spec
            var i = file.SitePath.LastIndexOf('.');
            var sitePath = i >= 0 ? file.SitePath.Substring(0, i) : file.SitePath;

            return (
                HashUtility.GetMd5Guid($"{depotName}|{sourcePath.ToLowerInvariant()}").ToString(),
                HashUtility.GetMd5Guid($"{depotName}|{sitePath.ToLowerInvariant()}").ToString());
        }

        private bool TryGetDocumentIdConfig(PathString path, out DocumentIdConfig result, out PathString remainingPath)
        {
            foreach (var (basePath, config) in _documentIdRules)
            {
                if (path.StartsWithPath(basePath, out remainingPath))
                {
                    result = config;
                    return true;
                }
            }
            result = default;
            remainingPath = default;
            return false;
        }

        private Document GetDocumentCore(FilePath path)
        {
            switch (path.Origin)
            {
                case FileOrigin.Fallback:
                    return CreateDocument(_fallbackDocset, path);

                case FileOrigin.Dependency:
                    return CreateDocument(_dependencyDocsets[path.DependencyName], path);

                default:
                    return CreateDocument(_docset, path);
            }
        }

        private Dictionary<string, Docset> LoadDependencies(RepositoryProvider repositoryProvider)
        {
            var result = new Dictionary<string, Docset>(_config.Dependencies.Count, PathUtility.PathComparer);

            foreach (var (name, dependency) in _config.Dependencies)
            {
                var (entry, repository) = repositoryProvider.GetRepositoryWithDocsetEntry(FileOrigin.Dependency, name);
                if (!string.IsNullOrEmpty(entry))
                {
                    result.TryAdd(name, new Docset(entry, repository));
                }
            }

            return result;
        }

        private Document CreateDocument(Docset docset, FilePath path)
        {
            var contentType = path.Origin == FileOrigin.Redirection ? ContentType.Redirection : GetContentType(path.Path);

            var mime = contentType == ContentType.Page ? ReadMimeFromFile(_input, path) : default;
            var isPage = (contentType == ContentType.Page || contentType == ContentType.Redirection) && _templateEngine.IsPage(mime);
            var isExperimental = Path.GetFileNameWithoutExtension(path.Path).EndsWith(".experimental", PathUtility.PathComparison);
            var routedFilePath = ApplyRoutes(path.Path);
            var sitePath = FilePathToSitePath(routedFilePath, contentType, mime, _config.OutputJson, _config.UglifyUrl, isPage);
            if (_config.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }

            var siteUrl = PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), contentType, mime, _config.OutputJson, isPage);
            var canonicalUrl = GetCanonicalUrl(siteUrl, sitePath, isExperimental, contentType, mime, isPage);

            return new Document(docset, path, sitePath, siteUrl, canonicalUrl, contentType, mime, isExperimental, isPage);
        }

        private static string FilePathToSitePath(string path, ContentType contentType, string? mime, bool json, bool uglifyUrl, bool isPage)
        {
            switch (contentType)
            {
                case ContentType.Page:
                    if (mime is null || isPage)
                    {
                        if (Path.GetFileNameWithoutExtension(path).Equals("index", PathUtility.PathComparison))
                        {
                            var extension = json ? ".json" : ".html";
                            return Path.Combine(Path.GetDirectoryName(path) ?? "", "index" + extension).Replace('\\', '/');
                        }
                        if (json)
                        {
                            return Path.ChangeExtension(path, ".json");
                        }
                        if (uglifyUrl)
                        {
                            return Path.ChangeExtension(path, ".html");
                        }
                        var fileName = Path.GetFileNameWithoutExtension(path).TrimEnd(' ', '.');
                        return Path.Combine(Path.GetDirectoryName(path) ?? "", fileName, "index.html").Replace('\\', '/');
                    }
                    return Path.ChangeExtension(path, ".json");
                case ContentType.TableOfContents:
                    return Path.ChangeExtension(path, ".json");
                default:
                    return path;
            }
        }

        private static string PathToAbsoluteUrl(string path, ContentType contentType, string? mime, bool json, bool isPage)
        {
            var url = PathToRelativeUrl(path, contentType, mime, json, isPage);
            return url == "./" ? "/" : "/" + url;
        }

        private static string PathToRelativeUrl(string path, ContentType contentType, string? mime, bool json, bool isPage)
        {
            var url = path.Replace('\\', '/');

            switch (contentType)
            {
                case ContentType.Redirection:
                case ContentType.Page:
                    if (mime is null || isPage)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (fileName.Equals("index", PathUtility.PathComparison))
                        {
                            var i = url.LastIndexOf('/');
                            return i >= 0 ? url.Substring(0, i + 1) : "./";
                        }
                        if (json)
                        {
                            var i = url.LastIndexOf('.');
                            return i >= 0 ? url.Substring(0, i) : url;
                        }
                        return url;
                    }
                    return url;
                default:
                    return url;
            }
        }

        private string GetCanonicalUrl(string siteUrl, string sitePath, bool isExperimental, ContentType contentType, string? mime, bool isPage)
        {
            if (isExperimental)
            {
                sitePath = ReplaceLast(sitePath, ".experimental", "");
                siteUrl = PathToAbsoluteUrl(sitePath, contentType, mime, _config.OutputJson, isPage);
            }

            return $"https://{_config.HostName}/{_localization.Locale}{siteUrl}";

            string ReplaceLast(string source, string find, string replace)
            {
                var i = source.LastIndexOf(find);
                return i >= 0 ? source.Remove(i, find.Length).Insert(i, replace) : source;
            }
        }

        private PathString ApplyRoutes(PathString path)
        {
            (path, _) = _buildScope.MapPath(path);

            // the latter rule takes precedence of the former rule
            foreach (var (source, dest) in _routes)
            {
                if (path.StartsWithPath(source, out var remainingPath))
                {
                    if (remainingPath.IsDefault)
                    {
                        return dest + path.GetFileName();
                    }
                    return dest + remainingPath;
                }
            }
            return path;
        }

        private static SourceInfo<string?> ReadMimeFromFile(Input input, FilePath filePath)
        {
            SourceInfo<string?> mime = default;

            if (filePath.EndsWith(".json"))
            {
                // TODO: we could have not depend on this exists check, but currently
                //       LinkResolver works with Document and return a Document for token files,
                //       thus we are forced to get the mime type of a token file here even if it's not useful.
                //
                //       After token resolve does not create Document, this Exists check can be removed.
                if (input.Exists(filePath))
                {
                    using var reader = input.ReadText(filePath);
                    mime = JsonUtility.ReadMime(reader, filePath);
                }
            }
            else if (filePath.EndsWith(".yml"))
            {
                if (input.Exists(filePath))
                {
                    using var reader = input.ReadText(filePath);
                    mime = new SourceInfo<string?>(YamlUtility.ReadMime(reader), new SourceInfo(filePath, 1, 1));
                }
            }

            return mime;
        }
    }
}
