// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        /// Gets the MIME type specifed in YAML header or JSON $schema.
        /// </summary>
        public string Mime { get; }

        /// <summary>
        /// Gets the schema identified by <see cref="Mime"/>.
        /// </summary>
        public Schema Schema { get; }

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
        ///  - Docs not start with '/'
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
        ///  - Does not escape with <see cref="HrefUtility.EscapeUrl(string)"/>
        /// </summary>
        public string SiteUrl { get; }

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
        /// Gets a value indicating whether the current document is schema data
        /// </summary>
        public bool IsSchemaData => Schema != null && Schema.Attribute as PageSchemaAttribute == null;

        private readonly Lazy<(string docId, string versionIndependentId)> _id;

        // TODO:
        // This is a temporary property just so that legacy can access OutputPath,
        // I'll slowly converge legacy into main build and remove this property eventually.
        // Do not use this property in main build.
        internal string OutputPath;

        /// <summary>
        /// Intentionally left as private. Use <see cref="Document.TryCreateFromFile(Docset, string)"/> instead.
        /// </summary>
        private Document(
            Docset docset,
            string filePath,
            string sitePath,
            string siteUrl,
            ContentType contentType,
            string mime,
            Schema schema,
            bool isExperimental,
            string redirectionUrl = null)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));
            Debug.Assert(ContentType == ContentType.Redirection ? redirectionUrl != null : true);

            Docset = docset;
            FilePath = filePath;
            SitePath = sitePath;
            SiteUrl = siteUrl;
            ContentType = contentType;
            Mime = mime;
            Schema = schema;
            IsExperimental = isExperimental;
            RedirectionUrl = redirectionUrl;

            _id = new Lazy<(string docId, string versionId)>(() => LoadDocumentId());

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
            var isConfigReference = docset.Config.Extend.Concat(docset.Config.GetFileReferences()).Contains(filePath, PathUtility.PathComparer);
            var type = isConfigReference ? ContentType.Unknown : GetContentType(filePath);
            var (mime, schema) = type == ContentType.Page ? Schema.ReadFromFile(Path.Combine(docset.DocsetPath, filePath)) : default;
            var isExperimental = Path.GetFileNameWithoutExtension(filePath).EndsWith(".experimental", PathUtility.PathComparison);
            var routedFilePath = ApplyRoutes(filePath, docset.Routes);

            var sitePath = FilePathToSitePath(routedFilePath, type, schema, docset.Config.Output.Json, docset.Config.Output.UglifyUrl);
            if (docset.Config.Output.LowerCaseUrl)
            {
                sitePath = sitePath.ToLowerInvariant();
            }

            var siteUrl = PathToAbsoluteUrl(sitePath, type, schema, docset.Config.Output.Json);
            var contentType = redirectionUrl != null ? ContentType.Redirection : type;

            if (contentType == ContentType.Redirection && type != ContentType.Page)
            {
                return (Errors.InvalidRedirection(filePath, type), null);
            }

            return (null, new Document(docset, filePath, sitePath, siteUrl, contentType, mime, schema, isExperimental, redirectionUrl));
        }

        /// <summary>
        /// Opens a new <see cref="Document"/> based on the path relative to docset.
        /// </summary>
        /// <param name="docset">The current docset</param>
        /// <param name="pathToDocset">The path relative to docset root</param>
        /// <returns>A new document, or null if not found</returns>
        public static Document TryCreateFromFile(Docset docset, string pathToDocset)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(pathToDocset));
            Debug.Assert(!Path.IsPathRooted(pathToDocset));

            pathToDocset = PathUtility.NormalizeFile(pathToDocset);

            if (docset.TryResolveDocset(pathToDocset, out var resolvedDocset))
            {
                var (error, file) = TryCreate(resolvedDocset, pathToDocset);
                return error == null ? file : null;
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

                var dependencyFile = TryCreateFromFile(dependentDocset, pathToDocset.Substring(dependencyName.Length));
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

        internal static string FilePathToSitePath(string path, ContentType contentType, Schema schema, bool json, bool uglifyUrl)
        {
            switch (contentType)
            {
                case ContentType.Page:
                    if (schema == null || schema.Attribute is PageSchemaAttribute)
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

        internal static string PathToAbsoluteUrl(string path, ContentType contentType, Schema schema, bool json)
        {
            var url = PathToRelativeUrl(path, contentType, schema, json);
            return url == "." ? "/" : "/" + url;
        }

        internal static string PathToRelativeUrl(string path, ContentType contentType, Schema schema, bool json)
        {
            var url = path.Replace('\\', '/');

            switch (contentType)
            {
                case ContentType.Page:
                    if (schema == null || schema.Attribute is PageSchemaAttribute)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (fileName.Equals("index", PathUtility.PathComparison))
                        {
                            var i = url.LastIndexOf('/');
                            return i >= 0 ? url.Substring(0, i + 1) : ".";
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

        private static string ApplyRoutes(string path, IReadOnlyDictionary<string, string> routes)
        {
            // the latter rule takes precedence of the former rule
            foreach (var (source, dest) in routes)
            {
                var result = ApplyRoutes(path, source, dest);
                if (result != null)
                {
                    return result.Replace('\\', '/');
                }
            }
            return path;
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

            // if source ends with index.yml, change it to index.md
            if ("index.yml".Equals(Path.GetFileName(sourcePath).ToLowerInvariant()))
            {
                sourcePath = Path.ChangeExtension(sourcePath, ".md");
            }

            // get set path from site path
            // site path doesn't contain version info according to the output spec
            var sitePathWithoutExtension = Path.Combine(Path.GetDirectoryName(SitePath), Path.GetFileNameWithoutExtension(SitePath));
            var sitePath = PathUtility.NormalizeFile(Path.GetRelativePath(Docset.Config.DocumentId.SiteBasePath, sitePathWithoutExtension));

            return (
                HashUtility.GetMd5Guid($"{depotName}|{sourcePath.ToLowerInvariant()}").ToString(),
                HashUtility.GetMd5Guid($"{depotName}|{sitePath.ToLowerInvariant()}").ToString());
        }
    }
}
