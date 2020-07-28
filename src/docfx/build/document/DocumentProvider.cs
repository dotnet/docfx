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
        private readonly ErrorBuilder _errors;
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
            ErrorBuilder errors,
            Config config,
            BuildOptions buildOptions,
            BuildScope buildScope,
            TemplateEngine templateEngine,
            MonikerProvider monikerProvider)
        {
            _errors = errors;
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
            var outputPath = file.SitePath;

            switch (file.ContentType)
            {
                case ContentType.Page:
                case ContentType.Redirection:
                    var fileExtension = _config.OutputType switch
                    {
                        OutputType.Html => file.IsHtml ? ".html" : ".json",
                        OutputType.Json => _config.Legacy ? ".raw.page.json" : ".json",
                        _ => throw new NotSupportedException(),
                    };
                    outputPath = Path.ChangeExtension(outputPath, fileExtension);
                    break;

                case ContentType.TableOfContents:
                    var tocExtension = _config.OutputType switch
                    {
                        OutputType.Html => file.IsHtml ? ".html" : ".json",
                        OutputType.Json => ".json",
                        _ => throw new NotSupportedException(),
                    };
                    outputPath = Path.ChangeExtension(outputPath, tocExtension);
                    break;
            }

            if (_config.OutputUrlType == OutputUrlType.Docs)
            {
                var monikers = _monikerProvider.GetFileLevelMonikers(_errors, path);
                outputPath = UrlUtility.Combine(monikers.MonikerGroup ?? "", outputPath);
            }

            return UrlUtility.Combine(_config.BasePath, outputPath);
        }

        public string GetDocsSiteUrl(FilePath path)
        {
            var file = GetDocument(path);
            if (_config.OutputUrlType == OutputUrlType.Docs)
            {
                return file.SiteUrl;
            }
            var sitePath = FilePathToSitePath(path, file.ContentType, OutputUrlType.Docs, file.IsHtml);
            return PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), file.ContentType, OutputUrlType.Docs, file.IsHtml);
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

            // if source is redirection or migrated from markdown, change it to *.md
            if (file.ContentType == ContentType.Redirection || TemplateEngine.IsMigratedFromMarkdown(file.Mime))
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
            var isHtml = _templateEngine.IsHtml(contentType, mime);
            var isExperimental = Path.GetFileNameWithoutExtension(path.Path).EndsWith(".experimental", PathUtility.PathComparison);
            var sitePath = FilePathToSitePath(path, contentType, _config.OutputUrlType, isHtml);
            var siteUrl = PathToAbsoluteUrl(Path.Combine(_config.BasePath, sitePath), contentType, _config.OutputUrlType, isHtml);
            var canonicalUrl = GetCanonicalUrl(siteUrl, sitePath, isExperimental, contentType, isHtml);

            return new Document(path, sitePath, siteUrl, canonicalUrl, contentType, mime, isExperimental, isHtml);
        }

        private string FilePathToSitePath(FilePath filePath, ContentType contentType, OutputUrlType outputUrlType, bool isHtml)
        {
            var sitePath = ApplyRoutes(filePath.Path).Value;
            if (contentType == ContentType.Page || contentType == ContentType.Redirection || contentType == ContentType.TableOfContents)
            {
                if (contentType == ContentType.Page && !isHtml)
                {
                    sitePath = Path.ChangeExtension(sitePath, ".json");
                }
                else
                {
                    sitePath = outputUrlType switch
                    {
                        OutputUrlType.Docs => Path.ChangeExtension(sitePath, ".json"),
                        OutputUrlType.Pretty => Path.GetFileNameWithoutExtension(sitePath).Equals("index", PathUtility.PathComparison)
                            ? Path.Combine(Path.GetDirectoryName(sitePath) ?? "", "index.html")
                            : Path.Combine(Path.GetDirectoryName(sitePath) ?? "", Path.GetFileNameWithoutExtension(sitePath).TrimEnd(' ', '.'), "index.html"),
                        OutputUrlType.Ugly => Path.ChangeExtension(sitePath, ".html"),
                        _ => throw new NotSupportedException(),
                    };
                }
            }

            if (outputUrlType != OutputUrlType.Docs)
            {
                var monikers = _monikerProvider.GetFileLevelMonikers(_errors, filePath);
                sitePath = Path.Combine(monikers.MonikerGroup ?? "", sitePath);
            }
            if (_config.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }
            return sitePath.Replace('\\', '/');
        }

        private static string PathToAbsoluteUrl(string path, ContentType contentType, OutputUrlType outputUrlType, bool isHtml)
        {
            var url = PathToRelativeUrl(path, contentType, outputUrlType, isHtml);
            return url == "./" ? "/" : "/" + url;
        }

        private static string PathToRelativeUrl(string path, ContentType contentType, OutputUrlType outputUrlType, bool isHtml)
        {
            var url = path.Replace('\\', '/');

            if (contentType == ContentType.Redirection || contentType == ContentType.TableOfContents || (contentType == ContentType.Page && isHtml))
            {
                if (outputUrlType != OutputUrlType.Ugly)
                {
                    if (Path.GetFileNameWithoutExtension(path).Equals("index", PathUtility.PathComparison))
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
        private string GetCanonicalUrl(string siteUrl, string sitePath, bool isExperimental, ContentType contentType, bool isHtml)
        {
            if (isExperimental)
            {
                sitePath = ReplaceLast(sitePath, ".experimental", "");
                siteUrl = PathToAbsoluteUrl(sitePath, contentType, _config.OutputUrlType, isHtml);
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
