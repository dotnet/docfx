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

        public Document(Docset docset, string filePath)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));

            Docset = docset;

            FilePath = PathUtility.NormalizeFile(filePath);
            ContentType = GetContentType(filePath);
            SiteUrl = GetSiteUrl(FilePath, ContentType, Docset.Config);
            SitePath = GetSitePath(SiteUrl, ContentType);
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

        /// <summary>
        /// Resolves a new <see cref="Document"/> based on the <paramref name="relativePath"/>
        /// relative to this <see cref="Document"/>.
        /// </summary>
        public Document TryResolveFile(string relativePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(relativePath));
            Debug.Assert(!Path.IsPathRooted(relativePath));

            return TryResolveFromPathToDocset(Docset, Path.Combine(Path.GetDirectoryName(FilePath), relativePath));
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

        internal static string GetSiteUrl(string path, ContentType contentType, Config config)
        {
            path = '/' + ApplyRoutes(path, config.Routes);

            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    var extensionIndex = path.LastIndexOf('.');
                    if (extensionIndex >= 0)
                    {
                        path = path.Substring(0, extensionIndex);
                    }
                    if (path.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
                    {
                        path = path.Substring(0, path.Length - 5);
                    }
                    return path;
                case ContentType.TableOfContents:
                    return Path.ChangeExtension(path, ".json");
                default:
                    return path;
            }
        }

        internal static string GetSitePath(string url, ContentType contentType)
        {
            Debug.Assert(url.StartsWith('/'));

            var path = url.Substring(1);
            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                    if (path.Length == 0 || path.EndsWith('/'))
                        return path + "index.json";
                    return path + ".json";
                default:
                    return path;
            }
        }

        /// <summary>
        /// Resolve a new <see cref="Document"/> based on the path relative to docset root
        /// </summary>
        /// <param name="docset">The current docset</param>
        /// <param name="path">The path relative to docset root</param>
        /// <returns>A new document</returns>
        internal static Document TryResolveFromPathToDocset(Docset docset, string path)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(path));
            Debug.Assert(!PathUtility.FilePathHasInvalidChars(path));

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

                var buildItem = TryResolveFromPathToDocset(dependentDocset, relativePathToDependentDocset);

                if (buildItem != null)
                {
                    return buildItem;
                }
            }

            return default;
        }

        private static string ApplyRoutes(string path, RouteConfig[] routes)
        {
            // the latter rule takes precedence of the former rule
            for (var i = routes.Length - 1; i >= 0; i--)
            {
                var result = routes[i].GetOutputPath(path);
                if (result != null)
                    return result.Replace('\\', '/');
            }
            return path;
        }

        private static bool IsValidRelativePath(string path)
        {
            return path != null &&
                path.IndexOf('\\') == -1 &&
                !path.StartsWith('/') &&
                !PathUtility.FilePathHasInvalidChars(path);
        }
    }
}
