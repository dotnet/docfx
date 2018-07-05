// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Docs not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string SitePath { get; }

        /// <summary>
        /// Gets the Url relative to site root that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Always start with '/'
        ///  - May end with '/' if it is index.html
        ///  - Does not escape with <see cref="HrefUtility.EscapeUrl(string)"/>
        /// </summary>
        public string SiteUrl { get; }

        /// <summary>
        /// Gets the output file path relative to output directory that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// Gets a value indicating whether if it master content
        /// </summary>
        public bool IsMasterContent { get; }

        /// <summary>
        /// Gets the document id and version independent id
        /// </summary>
        public (string id, string versionIndependentId) Id => _id.Value;

        /// <summary>
        /// Gets the redirection URL if <see cref="ContentType"/> is <see cref="ContentType.Redirection"/>
        /// </summary>
        public string RedirectionUrl { get; }

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
            bool isMasterContent,
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
            IsMasterContent = isMasterContent;
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
            var isMasterContent = type == ContentType.Markdown || type == ContentType.SchemaDocument;
            var routedFilePath = ApplyRoutes(filePath, docset.Config.Routes);

            var sitePath = FilePathToSitePath(routedFilePath, type);
            var siteUrl = PathToAbsoluteUrl(sitePath, type);
            var outputPath = sitePath;
            var contentType = redirectionUrl != null ? ContentType.Redirection : type;

            if (contentType == ContentType.Redirection && !isMasterContent)
            {
                return (Errors.InvalidRedirection(filePath, type), null);
            }

            return (null, new Document(docset, filePath, sitePath, siteUrl, outputPath, contentType, isMasterContent, redirectionUrl));
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
            var name = Path.GetFileNameWithoutExtension(path);

            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                if (name.Equals("toc", StringComparison.OrdinalIgnoreCase))
                {
                    return ContentType.TableOfContents;
                }
                return ContentType.Markdown;
            }

            if (path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (name.Equals("toc", StringComparison.OrdinalIgnoreCase))
                {
                    return ContentType.TableOfContents;
                }
                if (name.Equals("docfx", StringComparison.OrdinalIgnoreCase))
                {
                    return ContentType.Unknown;
                }
                return ContentType.SchemaDocument;
            }

            return ContentType.Asset;
        }

        internal static string FilePathToSitePath(string path, ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    if (Path.GetFileNameWithoutExtension(path).Equals("index", StringComparison.OrdinalIgnoreCase))
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
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    var extensionIndex = url.LastIndexOf('.');
                    if (extensionIndex >= 0)
                    {
                        url = url.Substring(0, extensionIndex);
                    }
                    if (url.Equals("index", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".";
                    }
                    if (url.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
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
            var depotName = !string.IsNullOrEmpty(Docset.Config.Product)
                ? $"{Docset.Config.Product}.{Docset.Config.Name}"
                : Docset.Config.Name;

            var sourcePath = string.IsNullOrEmpty(Docset.Config.DocumentId.SourceBasePath)
                ? FilePath
                : PathUtility.NormalizeFile(Path.GetRelativePath(Docset.Config.DocumentId.SourceBasePath, FilePath));

            // site path doesn't contain version info according to the output spec
            var sitePathWithoutExtension = Path.Combine(Path.GetDirectoryName(SitePath), Path.GetFileNameWithoutExtension(SitePath));
            var sitePath = string.IsNullOrEmpty(Docset.Config.DocumentId.SiteBasePath)
                ? PathUtility.NormalizeFile(sitePathWithoutExtension)
                : PathUtility.NormalizeFile(Path.GetRelativePath(Docset.Config.DocumentId.SiteBasePath, sitePathWithoutExtension));

            return (Md5($"{depotName}|{sourcePath.ToLowerInvariant()}"), Md5($"{depotName}|{sitePath.ToLowerInvariant()}"));

            string Md5(string input)
            {
#pragma warning disable CA5351 //Not used for encryption
                using (var md5 = MD5.Create())
#pragma warning restore CA5351
                {
                    return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(input))).ToString();
                }
            }
        }
    }
}
