// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Plugins;

    // TODO: support incremental and update lastmod only when src file is changed, blocked by the postprocessor incremental framework
    [Export(nameof(SitemapGenerator), typeof(IPostProcessor))]
    public class SitemapGenerator : IPostProcessor
    {
        private static readonly XNamespace Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private const string HtmlExtension = ".html";
        private const string SitemapName = "sitemap.xml";

        public string Name => nameof(SitemapGenerator);

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            return metadata;
        }

        public Manifest Process(Manifest manifest, string outputFolder)
        {
            if (string.IsNullOrEmpty(manifest.SitemapOptions?.BaseUrl))
            {
                return manifest;
            }

            if (!manifest.SitemapOptions.BaseUrl.EndsWith("/"))
            {
                manifest.SitemapOptions.BaseUrl += '/';
            }

            if (!Uri.TryCreate(manifest.SitemapOptions.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                Logger.LogWarning($"Base url {manifest.SitemapOptions.BaseUrl} is not in a valid uri format.");
                return manifest;
            }

            if (manifest.SitemapOptions.Priority.HasValue && (manifest.SitemapOptions.Priority < 0 || manifest.SitemapOptions.Priority > 1))
            {
                Logger.LogWarning($"Invalid priority {manifest.SitemapOptions.Priority}, priority must be between 0.0 and 1.0. Use default value 0.5 instead");
                manifest.SitemapOptions.Priority = 0.5;
            }

            var sitemapDocument = new XStreamingElement(Namespace + "urlset", GetElements(manifest, baseUri));

            var sitemapOutputFile = Path.Combine(outputFolder, SitemapName);
            Logger.LogInfo($"Sitemap file is successfully exported to {sitemapOutputFile}");
            sitemapDocument.Save(sitemapOutputFile);
            return manifest;
        }

        private IEnumerable<Tuple<string, OutputFileInfo>> GetHtmlOutputFiles(Manifest manifest)
        {
            if (manifest.Files == null)
            {
                yield break;
            }

            foreach(var file in manifest.Files)
            {
                if (file.DocumentType != "Toc"
                    && file.OutputFiles.TryGetValue(HtmlExtension, out var info) 
                    && !string.IsNullOrEmpty(info.RelativePath))
                {
                    yield return Tuple.Create(file.SourceRelativePath, info);
                }
            }
        }

        private IEnumerable<XElement> GetElements(Manifest manifest, Uri baseUri)
        {
            if (manifest.Files == null)
            {
                yield break;
            }

            foreach (var file in (from f in manifest.Files where f.DocumentType != "Toc" orderby f.SourceRelativePath select f))
            {
                if (file.OutputFiles.TryGetValue(HtmlExtension, out var info) && !string.IsNullOrEmpty(info.RelativePath))
                {
                    var options = GetOptions(manifest.SitemapOptions, file.SourceRelativePath);

                    var currentBaseUri = baseUri;
                    if (options.BaseUrl != manifest.SitemapOptions.BaseUrl)
                    {
                        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out currentBaseUri))
                        {
                            Logger.LogWarning($"Base url {options.BaseUrl} is not in a valid uri format, use base url from the default setting {manifest.SitemapOptions.BaseUrl} instead.");
                            currentBaseUri = baseUri;
                        }
                    }

                    yield return GetElement(info.RelativePath, currentBaseUri, options);
                }
            }
        }

        private XElement GetElement(string relativePath, Uri baseUri, SitemapElementOptions options)
        {
            var uri = new Uri(baseUri, relativePath);

            return new XElement
                 (Namespace + "url",
                 new XElement(Namespace + "loc", uri.AbsoluteUri),
                 new XElement(Namespace + "lastmod", (options.LastModified ?? DateTime.Now).ToString("yyyy-MM-ddThh:mm:ssK")),
                 new XElement(Namespace + "changefreq", (options.ChangeFrequency ?? PageChangeFrequency.Daily).ToString().ToLowerInvariant()),
                 new XElement(Namespace + "priority", options.Priority ?? 0.5)
                 );
        }

        private SitemapElementOptions GetOptions(SitemapOptions rootOptions, string sourcePath)
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
                if (!options.BaseUrl.EndsWith("/"))
                {
                    options.BaseUrl += '/';
                }
            }

            options.BaseUrl = options.BaseUrl ?? rootOptions.BaseUrl;
            options.ChangeFrequency = options.ChangeFrequency ?? rootOptions.ChangeFrequency;
            options.Priority = options.Priority ?? rootOptions.Priority;
            options.LastModified = options.LastModified ?? rootOptions.LastModified;

            if (options.Priority.HasValue && (options.Priority < 0 || options.Priority > 1))
            {
                Logger.LogWarning($"Invalid priority {options.Priority}, priority must be between 0.0 and 1.0. Use default value 0.5 instead.");
                options.Priority = null;
            }

            return options;
        }

        private SitemapElementOptions GetMatchingOptions(SitemapOptions options, string sourcePath)
        {
            if (options.FileOptions != null)
            {
                // As the latter one overrides the former one, match the pattern from latter to former
                for (var i = options.FileOptions.Count - 1; i >= 0; i--)
                {
                    var item = options.FileOptions[i];
                    var glob = new GlobMatcher(item.Key);
                    if (glob.Match(sourcePath))
                    {
                        return item.Value;
                    }
                }
            }

            return options;
        }
    }
}
