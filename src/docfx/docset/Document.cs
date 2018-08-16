// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Document
    {
        /// <summary>
        /// Gets the owning docset of this document. A document can only belong to one docset.
        /// </summary>
        public Docset Docset { get; }

        /// <summary>
        /// Gets the content type of this document.
        /// </summary>
        public ContentType ContentType { get; }

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
        ///       locale    moniker                 site-path
        ///       |-^-| |------^------| |----------------^----------------|
        /// _site/en-us/netstandard-2.0/dotnet/api/system.string/index.json
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Docs not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string SitePath { get; }

        /// <summary>
        /// Gets the Url relative to site root that is:
        ///       locale    moniker                 site-url
        ///       |-^-| |------^------| |----------------^----------------|
        /// _site/en-us/netstandard-2.0/dotnet/api/system.string/
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Always start with '/'
        ///  - May end with '/' if it is index.html
        ///  - Does not escape with <see cref="HrefUtility.EscapeUrl(string)"/>
        /// </summary>
        public string SiteUrl { get; }

        /// <summary>
        /// Gets the output file path relative to output directory that is:
        ///       |                output-path                            |
        ///       locale    moniker                 site-path
        ///       |-^-| |------^------| |----------------^----------------|
        /// _site/en-us/netstandard-2.0/dotnet/api/system.string/index.json
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string OutputPath { get; }

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

        private readonly Lazy<(string docId, string versionIndependentId)> _id;

        /// <summary>
        /// Intentionally left as private. Use <see cref="Document.TryCreateFromFile(Docset, string)"/> instead.
        /// </summary>
        private Document(
            Docset docset,
            string filePath,
            string sitePath,
            string siteUrl,
            string outputPath,
            ContentType contentType,
            bool isExperimental,
            string redirectionUrl = null)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));
            Debug.Assert(ContentType == ContentType.Redirection ? redirectionUrl != null : true);

            Docset = docset;
            FilePath = filePath;
            SitePath = sitePath;
            SiteUrl = siteUrl;
            OutputPath = outputPath;
            ContentType = contentType;
            IsExperimental = isExperimental;
            RedirectionUrl = redirectionUrl;

            _id = new Lazy<(string docId, string versionId)>(() => LoadDocumentId());

            Debug.Assert(IsValidRelativePath(FilePath));
            Debug.Assert(IsValidRelativePath(OutputPath));
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
            return File.OpenRead(Path.Combine(Docset.DocsetPath, FilePath));
        }

        /// <summary>
        /// Reads this document as text, throws if it does not exists.
        /// </summary>
        public string ReadText()
        {
            Debug.Assert(ContentType != ContentType.Redirection);
            using (var reader = new StreamReader(ReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Docset, PathUtility.PathComparer.GetHashCode(FilePath), ContentType);
        }

        public bool Equals(Document other)
        {
            if (other == null)
            {
                return false;
            }

            return Docset == other.Docset &&
                   ContentType == other.ContentType &&
                   string.Equals(FilePath, other.FilePath, PathUtility.PathComparison);
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
        public static (Error error, Document doc) TryCreate(Docset docset, string path, string redirectionUrl = null)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!Path.IsPathRooted(path));

            var filePath = PathUtility.NormalizeFile(path);
            var type = GetContentType(filePath);
            var isExperimental = Path.GetFileNameWithoutExtension(filePath).EndsWith(".experimental", PathUtility.PathComparison);
            var routedFilePath = ApplyRoutes(filePath, docset.Config.Routes);

            var sitePath = FilePathToSitePath(routedFilePath, type);
            var siteUrl = PathToAbsoluteUrl(sitePath, type);
            var outputPath = sitePath;
            var contentType = redirectionUrl != null ? ContentType.Redirection : type;

            if (contentType == ContentType.Redirection && type != ContentType.Page)
            {
                return (Errors.InvalidRedirection(filePath, type), null);
            }

            return (null, new Document(docset, filePath, sitePath, siteUrl, outputPath, contentType, isExperimental, redirectionUrl));
        }

        /// <summary>
        /// Opens a new <see cref="Document"/> based on the path relative to docset.
        /// </summary>
        /// <param name="docset">The current docset</param>
        /// <param name="path">The path relative to docset root</param>
        /// <returns>A new document, or null if not found</returns>
        public static Document TryCreateFromFile(Docset docset, string path)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!Path.IsPathRooted(path));

            path = PathUtility.NormalizeFile(path);

            // resolve from current docset
            if (File.Exists(Path.Combine(docset.DocsetPath, path)))
            {
                var (error, file) = TryCreate(docset, path);
                return error == null ? file : null;
            }

            // todo: localization fallback logic

            // resolve from dependent docsets
            foreach (var (dependencyName, dependentDocset) in docset.DependentDocset)
            {
                Debug.Assert(dependencyName.EndsWith('/'));

                if (!path.StartsWith(dependencyName, PathUtility.PathComparison))
                {
                    // the file stored in the dependent docset should start with dependency name
                    continue;
                }

                var dependencyFile = TryCreateFromFile(dependentDocset, path.Substring(dependencyName.Length));
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

        internal static string FilePathToSitePath(string path, ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Page:
                    if (Path.GetFileNameWithoutExtension(path).Equals("index", PathUtility.PathComparison))
                    {
                        return Path.Combine(Path.GetDirectoryName(path), "index.json").Replace('\\', '/');
                    }
                    return Path.ChangeExtension(path, ".json");
                case ContentType.TableOfContents:
                    return Path.ChangeExtension(path, ".json");
                default:
                    return path;
            }
        }

        internal static string PathToAbsoluteUrl(string path, ContentType contentType)
        {
            var url = PathToRelativeUrl(path, contentType);
            return url == "." ? "/" : "/" + url;
        }

        internal static string PathToRelativeUrl(string path, ContentType contentType)
        {
            var url = path.Replace('\\', '/');

            switch (contentType)
            {
                case ContentType.Page:
                    var extensionIndex = url.LastIndexOf('.');
                    if (extensionIndex >= 0)
                    {
                        url = url.Substring(0, extensionIndex);
                    }
                    if (url.Equals("index", PathUtility.PathComparison))
                    {
                        return ".";
                    }
                    if (url.EndsWith("/index", PathUtility.PathComparison))
                    {
                        return url.Substring(0, url.Length - 5);
                    }
                    return url;
                default:
                    return url;
            }
        }

        private static string ApplyRoutes(string path, RouteConfig[] routes)
        {
            // the latter rule takes precedence of the former rule
            for (var i = routes.Length - 1; i >= 0; i--)
            {
                var result = routes[i].GetOutputPath(path);
                if (result != null)
                {
                    return result.Replace('\\', '/');
                }
            }
            return path;
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

            // get set path from site path
            // site path doesn't contain version info according to the output spec
            var sitePathWithoutExtension = Path.Combine(Path.GetDirectoryName(SitePath), Path.GetFileNameWithoutExtension(SitePath));
            var sitePath = PathUtility.NormalizeFile(Path.GetRelativePath(Docset.Config.DocumentId.SiteBasePath, sitePathWithoutExtension));

            return (HashUtility.GetMd5String($"{depotName}|{sourcePath.ToLowerInvariant()}"), HashUtility.GetMd5String($"{depotName}|{sitePath.ToLowerInvariant()}"));
        }
    }
}
