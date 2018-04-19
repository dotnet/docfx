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
        /// Gets the output file path relative to output directory that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// Gets the Url relative to site root that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Always start with '/'
        ///  - May end with '/' if it is index.html
        /// </summary>
        public string SiteUrl { get; }

        public Document(Docset docset, string filePath)
        {
            Debug.Assert(!Path.IsPathRooted(filePath));

            Docset = docset;

            FilePath = PathUtility.NormalizeFile(filePath);
            ContentType = GetContentType(filePath, docset.DocsetPath);
            OutputPath = GetOutputPath(FilePath, ContentType);
            SiteUrl = GetSiteUrl(FilePath, ContentType);
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

        internal static ContentType GetContentType(string path, string docsetPath)
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
                        if (File.Exists(Path.Combine(docsetPath, Path.ChangeExtension(path, ".md"))))
                        {
                            // TODO: warn 'toc.md' is picked instead
                            return ContentType.Unknown;
                        }
                        return ContentType.TableOfContents;
                    }
                    return ContentType.SchemaDocument;
                default:
                    return ContentType.Asset;
            }
        }

        internal static string GetOutputPath(string path, ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Markdown:
                case ContentType.SchemaDocument:
                case ContentType.TableOfContents:
                    return Path.ChangeExtension(path, ".json");
                default:
                    return path;
            }
        }

        internal static string GetSiteUrl(string path, ContentType contentType)
        {
            path = '/' + path;

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
    }
}
