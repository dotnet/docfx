// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Xml.Linq;

using Docfx.Common;
using Docfx.Glob;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

[Export(nameof(SitemapGenerator), typeof(IPostProcessor))]
class SitemapGenerator : IPostProcessor
{
    private static readonly XNamespace Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9"; // lgtm [cs/non-https-url]
    private const string HtmlExtension = ".html";
    private const string SitemapName = "sitemap.xml";

    public string Name => nameof(SitemapGenerator);

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(manifest.Sitemap?.BaseUrl))
        {
            return manifest;
        }

        if (!manifest.Sitemap.BaseUrl.EndsWith('/'))
        {
            manifest.Sitemap.BaseUrl += '/';
        }

        if (!Uri.TryCreate(manifest.Sitemap.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            Logger.LogWarning($"Base url {manifest.Sitemap.BaseUrl} is not in a valid uri format.");
            return manifest;
        }

        if (manifest.Sitemap.Priority.HasValue && (manifest.Sitemap.Priority < 0 || manifest.Sitemap.Priority > 1))
        {
            Logger.LogWarning($"Invalid priority {manifest.Sitemap.Priority}, priority must be between 0.0 and 1.0. Use default value 0.5 instead");
            manifest.Sitemap.Priority = 0.5;
        }

        var sitemapDocument = new XStreamingElement(Namespace + "urlset", GetElements(manifest, baseUri));

        var sitemapOutputFile = Path.Combine(outputFolder, SitemapName);
        Logger.LogInfo($"Sitemap file is successfully exported to {sitemapOutputFile}");
        sitemapDocument.Save(sitemapOutputFile);
        return manifest;
    }

    private static IEnumerable<XElement> GetElements(Manifest manifest, Uri baseUri)
    {
        var sitemapOptions = manifest.Sitemap;
        var sitemapTargetFiles = GetManifestFilesForSitemap(manifest).OrderBy(x => x.relativeHtmlPath);

        foreach (var (relativeHtmlPath, file) in sitemapTargetFiles)
        {
            var options = GetOptions(sitemapOptions, file.SourceRelativePath);

            var currentBaseUri = baseUri;
            if (options.BaseUrl != sitemapOptions.BaseUrl && !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out currentBaseUri))
            {
                Logger.LogWarning($"Base url {options.BaseUrl} is not in a valid uri format, use base url from the default setting {manifest.Sitemap.BaseUrl} instead.");
                currentBaseUri = baseUri;
            }

            yield return GetElement(relativeHtmlPath, currentBaseUri, options);
        }
    }

    private static XElement GetElement(string relativePath, Uri baseUri, SitemapElementOptions options)
    {
        var uri = new Uri(baseUri, relativePath);

        return new XElement
             (Namespace + "url",
             new XElement(Namespace + "loc", uri.AbsoluteUri),
             new XElement(Namespace + "lastmod", (options.LastModified ?? DateTime.Now).ToString("yyyy-MM-ddThh:mm:ssK", CultureInfo.InvariantCulture)),
             new XElement(Namespace + "changefreq", (options.ChangeFrequency ?? PageChangeFrequency.Daily).ToString().ToLowerInvariant()),
             new XElement(Namespace + "priority", options.Priority ?? 0.5)
             );
    }

    private static SitemapElementOptions GetOptions(SitemapOptions rootOptions, string sourcePath)
    {
        var options = GetMatchingOptions(rootOptions, sourcePath);
        if (options == rootOptions)
        {
            return options;
        }

        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            options.BaseUrl = rootOptions.BaseUrl;
        }
        else
        {
            if (!options.BaseUrl.EndsWith('/'))
            {
                options.BaseUrl += '/';
            }
        }

        options.BaseUrl ??= rootOptions.BaseUrl;
        options.ChangeFrequency ??= rootOptions.ChangeFrequency;
        options.Priority ??= rootOptions.Priority;
        options.LastModified ??= rootOptions.LastModified;

        if (options.Priority.HasValue && (options.Priority < 0 || options.Priority > 1))
        {
            Logger.LogWarning($"Invalid priority {options.Priority}, priority must be between 0.0 and 1.0. Use default value 0.5 instead.");
            options.Priority = null;
        }

        return options;
    }

    private static SitemapElementOptions GetMatchingOptions(SitemapOptions options, string sourcePath)
    {
        if (options.FileOptions != null)
        {
            // As the latter one overrides the former one, match the pattern from latter to former
            foreach (var (key, value) in options.FileOptions.Reverse())
            {
                var glob = new GlobMatcher(key);
                if (glob.Match(sourcePath))
                {
                    return value;
                }
            }
        }

        return options;
    }

    private static IEnumerable<(string relativeHtmlPath, ManifestItem manifestItem)> GetManifestFilesForSitemap(Manifest manifest)
    {
        if (manifest.Files == null)
        {
            yield break;
        }

        foreach (var file in manifest.Files)
        {
            switch (file.Type)
            {
                // Skip non sitemap target files.
                case DataContracts.Common.Constants.DocumentType.Toc:
                case DataContracts.Common.Constants.DocumentType.Redirection:
                    continue;

                default:
                    break;
            }

            // Skip if manifest don't contains HTML output file.
            if (!file.Output.TryGetValue(HtmlExtension, out var info))
            {
                continue;
            }

            // Skip if output HTML relative path is empty.
            if (string.IsNullOrEmpty(info.RelativePath))
            {
                continue;
            }

            yield return (info.RelativePath, file);
        }
    }
}
