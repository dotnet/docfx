// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class Document : IEquatable<Document>, IComparable<Document>
    {
        /// <summary>
        /// Gets the content type of this document.
        /// </summary>
        public ContentType ContentType { get; }

        /// <summary>
        /// Gets the MIME type specified in YAML header or JSON $schema.
        /// </summary>
        public SourceInfo<string?> Mime { get; }

        /// <summary>
        /// Gets the source file path relative to docset folder that is:
        ///
        ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
        ///  - Does not start with '/'
        ///  - Does not end with '/'
        /// </summary>
        public FilePath FilePath { get; }

        /// <summary>
        /// Gets file path relative to <see cref="Config.BasePath"/> that is:
        /// For dynamic rendering:
        ///       locale  moniker-list-hash    site-path
        ///                       base_path
        ///       |-^-| |--^---| |--^--|----------^----------------|
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
        /// Gets the canonical URL
        /// </summary>
        public string CanonicalUrl { get; }

        /// <summary>
        /// Gets a value indicating whether the current document is output as HTML.
        /// </summary>
        public bool IsHtml { get; }

        /// <summary>
        /// Intentionally left as private. Use <see cref="Document.CreateFromFile(Docset, string)"/> instead.
        /// </summary>
        public Document(
            FilePath filePath,
            string sitePath,
            string siteUrl,
            string canonicalUrl,
            ContentType contentType,
            SourceInfo<string?> mime,
            bool isHtml = true)
        {
            Debug.Assert(!Path.IsPathRooted(filePath.Path));

            FilePath = filePath;
            SitePath = sitePath;
            SiteUrl = siteUrl;
            CanonicalUrl = canonicalUrl;
            ContentType = contentType;
            Mime = mime;
            IsHtml = isHtml;

            Debug.Assert(SiteUrl.StartsWith('/'));
            Debug.Assert(!SiteUrl.EndsWith('/') || Path.GetFileNameWithoutExtension(SitePath) == "index");
        }

        public int CompareTo(Document? other)
        {
            return other is null ? 1 : FilePath.CompareTo(other.FilePath);
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }

        public static bool operator ==(Document? a, Document? b) => Equals(a, b);

        public static bool operator !=(Document? a, Document? b) => !Equals(a, b);

        public bool Equals(Document? other)
        {
            if (other is null)
            {
                return false;
            }

            return FilePath.Equals(other.FilePath);
        }

        public override bool Equals(object? obj) => Equals(obj as Document);

        public override string ToString() => FilePath.ToString();
    }
}
