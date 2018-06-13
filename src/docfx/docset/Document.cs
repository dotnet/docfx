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
        /// Intentionally left as private. Use <see cref="Document.TryCreate(Docset, string)"/> instead.
        /// </summary>
        internal Document(Docset docset, string filePath)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));

            Docset = docset;

            FilePath = PathUtility.NormalizeFile(filePath);
            ContentType = GetContentType(filePath);

            var routedFilePath = ApplyRoutes(filePath, docset.Config.Routes);

            // TODO: handle URL escape
            SitePath = FilePathToSitePath(routedFilePath, ContentType);
            SiteUrl = PathToAbsoluteUrl(SitePath, ContentType);
            OutputPath = SitePath;

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
            return File.OpenRead(Path.Combine(Docset.DocsetPath, FilePath));
        }

        /// <summary>
        /// Reads this document as text, throws if it does not exists.
        /// </summary>
        public string ReadText()
        {
            using (var reader = new StreamReader(ReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(FilePath);
        }

        public bool Equals(Document other)
        {
            if (other == null)
            {
                return false;
            }

            return FilePath == other.FilePath && Docset == other.Docset;
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
        /// <returns>A new document, or null if not found</returns>
        public static Document TryCreate(Docset docset, string path)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!Path.IsPathRooted(path));

            path = PathUtility.NormalizeFile(path);

            // resolve from current docset
            if (File.Exists(Path.Combine(docset.DocsetPath, path)))
            {
                return new Document(docset, path);
            }

            // todo: localization fallback logic
            // todo: redirection files

            // resolve from dependent docsets
            foreach (var (dependencyName, url) in docset.Config.Dependencies)
            {
                if (!path.StartsWith(dependencyName, StringComparison.OrdinalIgnoreCase))
                {
                    // the file stored in the dependent docset should start with dependency name
                    continue;
                }

                var (docsetPath, _, _) = Restore.GetGitRestoreInfo(url);
                var dependentDocset = docset.DependentDocset[dependencyName];
                var relativePathToDependentDocset = Path.GetRelativePath(dependencyName, path);

                var dependencyFile = TryCreate(dependentDocset, relativePathToDependentDocset);
                if (dependencyFile != null)
                {
                    return dependencyFile;
                }
            }

            return default;
        }

        internal static ContentType GetContentType(string path)
        {
            var name = Path.GetFileName(path).ToLowerInvariant();
            var extension = Path.GetExtension(name);

            switch (extension)
            {
                case ".md":
                    if (name == "toc.md")
                    {
                        return ContentType.TableOfContents;
                    }
                    return ContentType.Markdown;
                case ".yml":
                    if (name == "docfx.yml")
                    {
                        return ContentType.Unknown;
                    }
                    if (name == "toc.yml")
                    {
                        return ContentType.TableOfContents;
                    }
                    return ContentType.SchemaDocument;
                default:
                    return ContentType.Asset;
            }
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
                    return Path.Combine(Path.GetDirectoryName(path), "toc.json").Replace('\\', '/');
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
            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    var extensionIndex = path.LastIndexOf('.');
                    if (extensionIndex >= 0)
                    {
                        path = path.Substring(0, extensionIndex);
                    }
                    if (path.Equals("index", StringComparison.OrdinalIgnoreCase))
                    {
                        return ".";
                    }
                    if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
                    {
                        return path.Substring(0, path.Length - 5);
                    }
                    return path;
                default:
                    return path;
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
    }
}
