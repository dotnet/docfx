// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.Docs.Build;

internal class Document
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
    /// Gets file path relative to <see cref="Config.BasePath"/> that is:
    /// For dynamic rendering:
    ///      locale base_path    site-path
    ///       |-^-| |--^--|----------^----------------|
    /// _site/en-us/dotnet/api/system.string/index.json
    ///
    ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
    ///  - Does not start with '/'
    ///  - Does not end with '/'
    /// </summary>
    public string SitePath { get; }

    /// <summary>
    /// Gets the Url relative to site root that is:
    /// For dynamic rendering:
    ///       locale            site-url
    ///       |-^-||----------------^------|
    /// _site/en-us/dotnet/api/system.string
    ///
    ///  - Normalized using <see cref="PathUtility.NormalizeFile(string)"/>
    ///  - Always start with '/'
    ///  - May end with '/' if it is index.html
    ///  - Does not escape with <see cref="UrlUtility.EscapeUrl(string)"/>
    /// </summary>
    public string SiteUrl { get; }

    /// <summary>
    /// Gets the output path.
    /// </summary>
    public string OutputPath { get; }

    /// <summary>
    /// Gets the canonical URL
    /// </summary>
    public string CanonicalUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the current document is output as HTML.
    /// </summary>
    public RenderType RenderType { get; }

    /// <summary>
    /// Intentionally left as private. Use <see cref="Document.CreateFromFile(Docset, string)"/> instead.
    /// </summary>
    public Document(
        string sitePath,
        string siteUrl,
        string outputPath,
        string canonicalUrl,
        ContentType contentType,
        SourceInfo<string?> mime,
        RenderType renderType)
    {
        SitePath = sitePath;
        SiteUrl = siteUrl;
        OutputPath = outputPath;
        CanonicalUrl = canonicalUrl;
        ContentType = contentType;
        Mime = mime;
        RenderType = renderType;

        Debug.Assert(SiteUrl.StartsWith('/'));
        Debug.Assert(!SiteUrl.EndsWith('/') || Path.GetFileNameWithoutExtension(SitePath) == "index");
    }
}
