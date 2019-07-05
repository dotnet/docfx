// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class Document : IEquatable<Document>, IComparable<Document>
    {
        /// <summary>
        /// Gets the owning docset of this document. A document can only belong to one docset.
        /// TODO: Split data and behaviorial objects from Document and Docset
        /// </summary>
        public Docset Docset { get; }

        /// <summary>
        /// Gets the content type of this document.
        /// </summary>
        public ContentType ContentType { get; }

        /// <summary>
        /// Gets the MIME type specifed in YAML header or JSON $schema.
        /// </summary>
        public SourceInfo<string> Mime { get; }

        /// <summary>
        /// Gets the source file path relative to docset folder that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets file path relative to site root that is:
        /// For dynamic rendering:
        ///       locale  moniker-list-hash    site-path
        ///       |-^-| |--^---| |----------------^----------------|
        /// _site/en-us/603b739b/dotnet/api/system.string/index.json
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string SitePath { get; }

        /// <summary>
        /// Gets the Url relative to site root that is:
        /// For dynamic rendering:
        ///       locale moniker-list-hash    site-url
        ///       |-^-| |---^--| |----------------^-----|
        /// _site/en-us/603b739b/dotnet/api/system.string
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Always start with '/'
        ///  - May end with '/' if it is index.html
        ///  - Does not escape with <see cref="UrlUtility.EscapeUrl(string)"/>
        /// </summary>
        public string SiteUrl { get; }

        /// <summary>
        /// Gets the canonical URL without locale
        /// </summary>
        public string CanonicalUrlWithoutLocale { get; }

        /// <summary>
        /// Gets the canonical URL
        /// </summary>
        public string CanonicalUrl { get; }

        /// <summary>
        /// Gets the document id and version independent id
        /// </summary>
        public (string id, string versionIndependentId) Id => _id.Value;

        /// <summary>
        /// Gets the redirection URL if <see cref="ContentType"/> is <see cref="ContentType.Redirection"/>
        /// </summary>
        public string RedirectionUrl { get; }

        /// <summary>
        /// Gets a value indicating whether it's an experimental content
        /// </summary>
        public bool IsExperimental { get; }

        /// <summary>
        /// Gets a value indicating whether it's from git history(deleted/moved/renamed)
        /// </summary>
        public bool IsFromHistory { get; }

        /// <summary>
        /// Gets a value indicating whether the current document is schema data
        /// </summary>
        public bool IsPage { get; }

        /// <summary>
        /// Gets the repository
        /// </summary>
        public Repository Repository => _repository.Value;

        private readonly Lazy<(string docId, string versionIndependentId)> _id;
        private readonly Lazy<Repository> _repository;

        /// <summary>
        /// Intentionally left as private. Use <see cref="Document.CreateFromFile(Docset, string)"/> instead.
        /// </summary>
        private Document(
            Docset docset,
            string filePath,
            string sitePath,
            string siteUrl,
            string canonicalUrlWithoutLocale,
            string canonicalUrl,
            ContentType contentType,
            SourceInfo<string> mime,
            bool isExperimental,
            string redirectionUrl = null,
            bool isFromHistory = false,
            bool isPage = true)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));
            Debug.Assert(ContentType == ContentType.Redirection ? redirectionUrl != null : true);

            Docset = docset;
            FilePath = filePath;
            SitePath = sitePath;
            SiteUrl = siteUrl;
            CanonicalUrlWithoutLocale = canonicalUrlWithoutLocale;
            CanonicalUrl = canonicalUrl;
            ContentType = contentType;
            Mime = mime;
            IsExperimental = isExperimental;
            RedirectionUrl = redirectionUrl;
            IsFromHistory = isFromHistory;
            IsPage = isPage;

            _id = new Lazy<(string docId, string versionId)>(() => LoadDocumentId());
            _repository = new Lazy<Repository>(() => Docset.GetRepository(FilePath));

            Debug.Assert(IsValidRelativePath(FilePath));
            Debug.Assert(IsValidRelativePath(SitePath));

            Debug.Assert(SiteUrl.StartsWith('/'));
            Debug.Assert(!SiteUrl.EndsWith('/') || Path.GetFileNameWithoutExtension(SitePath) == "index");
        }

        /// <summary>
        /// Reads this document as stream, throws if it does not exists.
        /// </summary>
        public Stream ReadStream()
        {
            Debug.Assert(ContentType != ContentType.Redirection);
            Debug.Assert(!IsFromHistory);

            return File.OpenRead(Path.Combine(Docset.DocsetPath, FilePath));
        }

        /// <summary>
        /// Reads this document as text, throws if it does not exists.
        /// </summary>
        public string ReadText()
        {
            Debug.Assert(ContentType != ContentType.Redirection);
            Debug.Assert(!IsFromHistory);

            using (var reader = new StreamReader(ReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public string GetOutputPath(List<string> monikers, string siteBasePath, bool isPage = true)
        {
            var outputPath = PathUtility.NormalizeFile(Path.Combine(
                siteBasePath,
                $"{MonikerUtility.GetGroup(monikers)}",
                Path.GetRelativePath(siteBasePath, SitePath)));

            return Docset.Legacy && isPage ? LegacyUtility.ChangeExtension(outputPath, ".raw.page.json") : outputPath;
        }

        public int CompareTo(Document other)
        {
            var result = PathUtility.PathComparer.Compare(Docset.DocsetPath, other.Docset.DocsetPath);
            if (result == 0)
                result = ContentType.CompareTo(other.ContentType);
            if (result == 0)
                result = PathUtility.PathComparer.Compare(FilePath, other.FilePath);
            return result;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                PathUtility.PathComparer.GetHashCode(Docset.DocsetPath),
                PathUtility.PathComparer.GetHashCode(FilePath),
                ContentType);
        }

        public bool Equals(Document other)
        {
            if (other is null)
            {
                return false;
            }

            return string.Equals(Docset.DocsetPath, other.Docset.DocsetPath, PathUtility.PathComparison) &&
                   string.Equals(FilePath, other.FilePath, PathUtility.PathComparison) &&
                   ContentType == other.ContentType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Document);
        }

        public override string ToString()
        {
            return FilePath;
        }

        public static bool operator ==(Document obj1, Document obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(Document obj1, Document obj2)
        {
            return !Equals(obj1, obj2);
        }

        /// <summary>
        /// Opens a new <see cref="Document"/> based on the path relative to docset.
        /// </summary>
        /// <param name="docset">The current docset</param>
        /// <param name="path">The path relative to docset root</param>
        public static Document Create(Docset docset, string path, TemplateEngine templateEngine, string redirectionUrl = null, bool isFromHistory = false, bool combineRedirectUrl = false)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!Path.IsPathRooted(path));

            var filePath = PathUtility.NormalizeFile(path);
            var isConfigReference = docset.Config.Extend.Concat(docset.Config.GetFileReferences()).Contains(filePath, PathUtility.PathComparer);
            var type = isConfigReference ? ContentType.Unknown : GetContentType(filePath);
            var mime = type == ContentType.Page ? ReadMimeFromFile(filePath, Path.Combine(docset.DocsetPath, filePath)) : default;
            var isPage = templateEngine.IsPage(mime);
            var isExperimental = Path.GetFileNameWithoutExtension(filePath).EndsWith(".experimental", PathUtility.PathComparison);
            var routedFilePath = ApplyRoutes(filePath, docset.Routes, docset.SiteBasePath);

            var sitePath = FilePathToSitePath(routedFilePath, type, mime, docset.Config.Output.Json, docset.Config.Output.UglifyUrl, isPage);
            if (docset.Config.Output.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }

            var siteUrl = PathToAbsoluteUrl(sitePath, type, mime, docset.Config.Output.Json, isPage);
            var contentType = type;
            if (redirectionUrl != null)
            {
                contentType = ContentType.Redirection;
                redirectionUrl = combineRedirectUrl ? PathUtility.Normalize(Path.Combine(Path.GetDirectoryName(siteUrl), redirectionUrl)) : redirectionUrl;
                redirectionUrl = redirectionUrl.EndsWith("/index") ? redirectionUrl.Substring(0, redirectionUrl.Length - "index".Length) : redirectionUrl;
            }
            var canonicalUrl = GetCanonicalUrl(siteUrl, sitePath, docset, isExperimental, contentType, mime, isPage);
            var canonicalUrlWithoutLocale = GetCanonicalUrl(siteUrl, sitePath, docset, isExperimental, contentType, mime, isPage, withLocale: false);

            return new Document(docset, filePath, sitePath, siteUrl, canonicalUrlWithoutLocale, canonicalUrl, contentType, mime, isExperimental, redirectionUrl, isFromHistory, isPage);
        }

        /// <summary>
        /// Opens a new <see cref="Document"/> based on the path relative to docset.
        /// </summary>
        /// <param name="docset">The current docset</param>
        /// <param name="pathToDocset">The path relative to docset root</param>
        /// <returns>A new document, or null if not found</returns>
        public static Document CreateFromFile(Docset docset, string pathToDocset, TemplateEngine templateEngine)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(pathToDocset));
            Debug.Assert(!Path.IsPathRooted(pathToDocset));

            pathToDocset = PathUtility.NormalizeFile(pathToDocset);

            if (TryResolveDocset(docset, pathToDocset, out var resolvedDocset))
            {
                return Create(resolvedDocset, pathToDocset, templateEngine);
            }

            // resolve from dependent docsets
            foreach (var (dependencyName, dependentDocset) in docset.DependencyDocsets)
            {
                Debug.Assert(dependencyName.EndsWith('/'));

                if (!pathToDocset.StartsWith(dependencyName, PathUtility.PathComparison))
                {
                    // the file stored in the dependent docset should start with dependency name
                    continue;
                }

                var dependencyFile = CreateFromFile(dependentDocset, pathToDocset.Substring(dependencyName.Length), templateEngine);
                if (dependencyFile != null)
                {
                    return dependencyFile;
                }
            }

            return default;
        }

        internal static ContentType GetContentType(string path)
        {
            if (!path.EndsWith(".md", PathUtility.PathComparison) &&
                !path.EndsWith(".json", PathUtility.PathComparison) &&
                !path.EndsWith(".yml", PathUtility.PathComparison))
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

            return ContentType.Page;
        }

        internal static string FilePathToSitePath(string path, ContentType contentType, string mime, bool json, bool uglifyUrl, bool isPage)
        {
            switch (contentType)
            {
                case ContentType.Page:
                    if (mime is null || isPage)
                    {
                        if (Path.GetFileNameWithoutExtension(path).Equals("index", PathUtility.PathComparison))
                        {
                            var extension = json ? ".json" : ".html";
                            return Path.Combine(Path.GetDirectoryName(path), "index" + extension).Replace('\\', '/');
                        }
                        if (json)
                        {
                            return Path.ChangeExtension(path, ".json");
                        }
                        if (uglifyUrl)
                        {
                            return Path.ChangeExtension(path, ".html");
                        }
                        return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), "index.html").Replace('\\', '/');
                    }
                    return Path.ChangeExtension(path, ".json");
                case ContentType.TableOfContents:
                    return Path.ChangeExtension(path, ".json");
                default:
                    return path;
            }
        }

        internal static string PathToAbsoluteUrl(string path, ContentType contentType, string mime, bool json, bool isPage)
        {
            var url = PathToRelativeUrl(path, contentType, mime, json, isPage);
            return url == "./" ? "/" : "/" + url;
        }

        internal static string PathToRelativeUrl(string path, ContentType contentType, string mime, bool json, bool isPage)
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

        private static string GetCanonicalUrl(string siteUrl, string sitePath, Docset docset, bool isExperimental, ContentType contentType, string mime, bool isPage, bool withLocale = true)
        {
            var config = docset.Config;
            if (isExperimental)
            {
                sitePath = ReplaceLast(sitePath, ".experimental", "");
                siteUrl = PathToAbsoluteUrl(sitePath, contentType, mime, config.Output.Json, isPage);
            }

            return withLocale ? $"{docset.HostName}/{docset.Locale}{siteUrl}" : $"{config.BaseUrl}{siteUrl}";

            string ReplaceLast(string source, string find, string replace)
            {
                var i = source.LastIndexOf(find);
                return i >= 0 ? source.Remove(i, find.Length).Insert(i, replace) : source;
            }
        }

        private static string ApplyRoutes(string path, IReadOnlyDictionary<string, string> routes, string siteBasePath)
        {
            // the latter rule takes precedence of the former rule
            foreach (var (source, dest) in routes)
            {
                var result = ApplyRoutes(path, source, dest);
                if (result != null)
                {
                    path = result;
                    break;
                }
            }
            return PathUtility.NormalizeFile(Path.Combine(siteBasePath, path));
        }

        private static string ApplyRoutes(string path, string source, string dest)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            var (match, isFileMatch, remainingPath) = PathUtility.Match(path, source);

            if (match)
            {
                if (isFileMatch)
                {
                    return Path.Combine(dest, Path.GetFileName(path));
                }

                return Path.Combine(dest, remainingPath);
            }

            return null;
        }

        private static bool IsValidRelativePath(string path)
        {
            return path != null && path.IndexOf('\\') == -1 && !path.StartsWith('/');
        }

        private (string docId, string versionIndependentId) LoadDocumentId()
        {
            var sourcePath = PathUtility.NormalizeFile(Path.GetRelativePath(Docset.Config.DocumentId.SourceBasePath, FilePath));

            var (mappedDepotName, mappedSourcePath) = Docset.Config.DocumentId.GetMapping(sourcePath);

            // get depot name from config or depot mapping
            var depotName = string.IsNullOrEmpty(mappedDepotName)
                ? !string.IsNullOrEmpty(Docset.Config.Product)
                    ? $"{Docset.Config.Product}.{Docset.Config.Name}"
                    : Docset.Config.Name
                : mappedDepotName;

            // get source path from source file path or directory mapping
            sourcePath = string.IsNullOrEmpty(mappedSourcePath)
                ? sourcePath
                : mappedSourcePath;

            // if source is landing page, change it to *.md
            if (TemplateEngine.IsLandingData(Mime))
            {
                sourcePath = Path.ChangeExtension(sourcePath, ".md");
            }

            // get set path from site path
            // site path doesn't contain version info according to the output spec
            var sitePathWithoutExtension = Path.Combine(Path.GetDirectoryName(SitePath), Path.GetFileNameWithoutExtension(SitePath));
            var sitePath = PathUtility.NormalizeFile(Path.GetRelativePath(Docset.SiteBasePath, sitePathWithoutExtension));

            return (
                HashUtility.GetMd5Guid($"{depotName}|{sourcePath.ToLowerInvariant()}").ToString(),
                HashUtility.GetMd5Guid($"{depotName}|{sitePath.ToLowerInvariant()}").ToString());
        }

        private static bool TryResolveDocset(Docset docset, string file, out Docset resolvedDocset)
        {
            // resolve from localization docset
            if (docset.LocalizationDocset != null && File.Exists(Path.Combine(docset.LocalizationDocset.DocsetPath, file)))
            {
                resolvedDocset = docset.LocalizationDocset;
                return true;
            }

            // resolve from current docset
            if (File.Exists(Path.Combine(docset.DocsetPath, file)))
            {
                resolvedDocset = docset;
                return true;
            }

            // resolve from fallback docset
            if (docset.FallbackDocset != null && File.Exists(Path.Combine(docset.FallbackDocset.DocsetPath, file)))
            {
                resolvedDocset = docset.FallbackDocset;
                return true;
            }

            resolvedDocset = null;
            return false;
        }

        private static SourceInfo<string> ReadMimeFromFile(string pathToDocset, string filePath)
        {
            SourceInfo<string> mime = default;

            if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = JsonUtility.ReadMime(reader, pathToDocset);
                    }
                }
            }
            else if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                if (File.Exists(filePath))
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        mime = new SourceInfo<string>(YamlUtility.ReadMime(reader), new SourceInfo(pathToDocset, 1, 1));
                    }
                }
            }

            return mime;
        }
    }
}
