// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DocumentProvider
    {
        private readonly Config _config;
        private readonly BuildScope _buildScope;
        private readonly BuildOptions _buildOptions;
        private readonly TemplateEngine _templateEngine;
        private readonly MonikerProvider _monikerProvider;

        private readonly string _depotName;
        private readonly (PathString, DocumentIdConfig)[] _documentIdRules;
        private readonly (PathString src, PathString dest)[] _routes;

        private readonly ConcurrentDictionary<FilePath, Document> _documents = new ConcurrentDictionary<FilePath, Document>();

        public DocumentProvider(
            Config config, BuildOptions buildOptions, BuildScope buildScope, TemplateEngine templateEngine, MonikerProvider monikerProvider)
        {
            _config = config;
            _buildOptions = buildOptions;
            _buildScope = buildScope;
            _templateEngine = templateEngine;
            _monikerProvider = monikerProvider;

            var documentIdConfig = config.GlobalMetadata.DocumentIdDepotMapping ?? config.DocumentId;
            _depotName = string.IsNullOrEmpty(config.Product) ? config.Name : $"{config.Product}.{config.Name}";
            _documentIdRules = documentIdConfig.Select(item => (item.Key, item.Value)).OrderByDescending(item => item.Key).ToArray();
            _routes = config.Routes.Reverse().Select(item => (item.Key, item.Value)).ToArray();
        }

        public Document GetDocument(FilePath path)
        {
            return _documents.GetOrAdd(path, GetDocumentCore);
        }

        public string GetOutputPath(FilePath path)
        {
            var file = GetDocument(path);
            string outputPath;

            if (_config.StaticOutput)
            {
                outputPath = UrlUtility.Combine(_config.BasePath, file.SitePath);
            }
            else
            {
                var (_, monikers) = _monikerProvider.GetFileLevelMonikers(path);
                outputPath = UrlUtility.Combine(_config.BasePath, MonikerUtility.GetGroup(monikers) ?? "", file.SitePath);
            }

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
                        ? config.FolderRelativePathInDocset.Value.Concat(file.FilePath.Path.GetFileName())
                        : config.FolderRelativePathInDocset.Value.Concat(remainingPath);
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
            var contentType = _buildScope.GetContentType(path);
            var mime = _buildScope.GetMime(contentType, path);
            var isPage = (contentType == ContentType.Page || contentType == ContentType.Redirection) && _templateEngine.IsPage(mime);
            var isExperimental = Path.GetFileNameWithoutExtension(path.Path).EndsWith(".experimental", PathUtility.PathComparison);
            var routedFilePath = ApplyRoutes(path.Path);
            var sitePath = FilePathToSitePath(routedFilePath, path, contentType, isPage);
            var siteUrl = PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), contentType, _config.StaticOutput, _config.UglifyUrl, isPage);
            var canonicalUrl = GetCanonicalUrl(siteUrl, sitePath, isExperimental, contentType, isPage);

            return new Document(path, sitePath, siteUrl, canonicalUrl, contentType, mime, isExperimental, isPage);
        }

        private string FilePathToSitePath(string path, FilePath filePath, ContentType contentType, bool isPage)
        {
            var sitePath = path;

            if (contentType == ContentType.Page || contentType == ContentType.Redirection || contentType == ContentType.TableOfContents)
            {
                if ((contentType == ContentType.Page && !isPage) || !_config.StaticOutput)
                {
                    sitePath = Path.ChangeExtension(path, ".json");
                }
                else if (_config.UglifyUrl)
                {
                    sitePath = Path.ChangeExtension(path, ".html");
                }
                else
                {
                    var fileName = Path.GetFileNameWithoutExtension(path).TrimEnd(' ', '.');
                    sitePath = Path.Combine(Path.GetDirectoryName(path) ?? "", fileName, "index.html").Replace('\\', '/');
                }
            }

            if (_config.StaticOutput)
            {
                var (_, monikers) = _monikerProvider.GetFileLevelMonikers(filePath);
                if (monikers.Length > 0)
                {
                    sitePath = $"{MonikerUtility.GetGroup(monikers)}/{sitePath}";
                }
            }
            if (_config.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }
            return sitePath;
        }

        private static string PathToAbsoluteUrl(string path, ContentType contentType, bool staticOutput, bool uglifyUrl, bool isPage)
        {
            var url = PathToRelativeUrl(path, contentType, staticOutput, uglifyUrl, isPage);
            return url == "./" ? "/" : "/" + url;
        }

        private static string PathToRelativeUrl(string path, ContentType contentType, bool staticOutput, bool uglifyUrl, bool isPage)
        {
            var url = path.Replace('\\', '/');

            switch (contentType)
            {
                case ContentType.Redirection:
                case ContentType.Page:
                    if (isPage)
                    {
                        if (!staticOutput || !uglifyUrl)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(path);
                            if (fileName.Equals("index", PathUtility.PathComparison))
                            {
                                var i = url.LastIndexOf('/');
                                return i >= 0 ? url.Substring(0, i + 1) : "./";
                            }
                        }
                        if (!staticOutput)
                        {
                            var i = url.LastIndexOf('.');
                            return i >= 0 ? url.Substring(0, i) : url;
                        }

                        return url;
                    }
                    return url;
                case ContentType.TableOfContents:
                    if (staticOutput && !uglifyUrl)
                    {
                        var i = url.LastIndexOf('/');
                        return url.Substring(0, i + 1);
                    }
                    return url;
                default:
                    return url;
            }
        }

        /// <summary>
        /// In docs, canonical URL is later overwritten by template JINT code.
        /// TODO: need to handle the logic difference when template code is removed.
        /// </summary>
        private string GetCanonicalUrl(string siteUrl, string sitePath, bool isExperimental, ContentType contentType, bool isPage)
        {
            if (isExperimental)
            {
                sitePath = ReplaceLast(sitePath, ".experimental", "");
                siteUrl = PathToAbsoluteUrl(sitePath, contentType, _config.StaticOutput, _config.UglifyUrl, isPage);
            }

            return $"https://{_config.HostName}/{_buildOptions.Locale}{siteUrl}";

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
                        return dest.Concat(path.GetFileName());
                    }
                    return dest.Concat(remainingPath);
                }
            }
            return path;
        }
    }
}
