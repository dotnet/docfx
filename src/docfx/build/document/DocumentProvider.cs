// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.Docs.Build
{
    internal class DocumentProvider
    {
        private readonly Config _config;
        private readonly BuildScope _buildScope;
        private readonly BuildOptions _buildOptions;
        private readonly TemplateEngine _templateEngine;
        private readonly MonikerProvider _monikerProvider;
        private readonly ErrorLog _errorLog;

        private readonly string _depotName;
        private readonly (PathString, DocumentIdConfig)[] _documentIdRules;
        private readonly (PathString src, PathString dest)[] _routes;

        private readonly ConcurrentDictionary<FilePath, Document> _documents = new ConcurrentDictionary<FilePath, Document>();

        public DocumentProvider(
            Config config, BuildOptions buildOptions, BuildScope buildScope, TemplateEngine templateEngine, MonikerProvider monikerProvider, ErrorLog errorLog)
        {
            _config = config;
            _buildOptions = buildOptions;
            _buildScope = buildScope;
            _templateEngine = templateEngine;
            _monikerProvider = monikerProvider;
            _errorLog = errorLog;

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

            if (_config.OutputUrlType != OutputUrlType.Docs)
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
            var siteUrl = PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), contentType, _config.OutputUrlType, isPage);
            var canonicalUrl = GetCanonicalUrl(siteUrl, sitePath, isExperimental, contentType, isPage);

            return new Document(path, sitePath, siteUrl, canonicalUrl, contentType, mime, isExperimental, isPage);
        }

        private string FilePathToSitePath(string path, FilePath filePath, ContentType contentType, bool isPage)
        {
            var pathExtension = contentType == ContentType.Page || contentType == ContentType.Redirection || contentType == ContentType.TableOfContents
                ? _config.OutputType switch
                {
                    OutputType.Html when contentType == ContentType.Page && !isPage => ".json",
                    OutputType.Html => ".html",
                    OutputType.Json => ".json",
                    _ => throw new NotSupportedException(),
                }
                : Path.GetExtension(path);
            var directoryName = Path.GetDirectoryName(path) ?? "";
            var fileName = Path.GetFileNameWithoutExtension(path).TrimEnd(' ', '.');
            var monikers = Array.Empty<string>();
            try
            {
                (_, monikers) = _monikerProvider.GetFileLevelMonikers(filePath);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errorLog.Write(dex);
            }
            var group = MonikerUtility.GetGroup(monikers) ?? "";
            var sitePath = _config.OutputUrlType switch
            {
                OutputUrlType.Docs => Path.Combine(directoryName, fileName + pathExtension).Replace('\\', '/'),
                OutputUrlType.Static => Path.Combine(group, directoryName, fileName, "index" + pathExtension).Replace('\\', '/'),
                OutputUrlType.StaticUgly => Path.Combine(group, directoryName, fileName + pathExtension).Replace('\\', '/'),
                _ => throw new NotSupportedException(),
            };

            if (_config.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }
            return sitePath;
        }

        private static string PathToAbsoluteUrl(string path, ContentType contentType, OutputUrlType outputUrlType, bool isPage)
        {
            var url = PathToRelativeUrl(path, contentType, outputUrlType, isPage);
            return url == "./" ? "/" : "/" + url;
        }

        private static string PathToRelativeUrl(string path, ContentType contentType, OutputUrlType outputUrlType, bool isPage)
        {
            var url = path.Replace('\\', '/');

            if (contentType == ContentType.Redirection || contentType == ContentType.TableOfContents || (contentType == ContentType.Page && isPage))
            {
                if (outputUrlType == OutputUrlType.Docs || outputUrlType == OutputUrlType.Static)
                {
                    var fileName = Path.GetFileNameWithoutExtension(path);
                    if (fileName.Equals("index", PathUtility.PathComparison))
                    {
                        var i = url.LastIndexOf('/');
                        return i >= 0 ? url.Substring(0, i + 1) : "./";
                    }
                }
                if (outputUrlType == OutputUrlType.Docs && contentType != ContentType.TableOfContents)
                {
                    var i = url.LastIndexOf('.');
                    return i >= 0 ? url.Substring(0, i) : url;
                }
            }
            return url;
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
                siteUrl = PathToAbsoluteUrl(sitePath, contentType, _config.OutputUrlType, isPage);
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
