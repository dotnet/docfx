// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LinkResolver
    {
        private readonly Config _config;
        private readonly BuildOptions _buildOptions;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly DocumentProvider _documentProvider;
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly XrefResolver _xrefResolver;
        private readonly TemplateEngine _templateEngine;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;
        private readonly MetadataProvider _metadataProvider;

        public LinkResolver(
            Config config,
            BuildOptions buildOptions,
            BuildScope buildScope,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            XrefResolver xrefResolver,
            TemplateEngine templateEngine,
            FileLinkMapBuilder fileLinkMapBuilder,
            MetadataProvider metadataProvider)
        {
            _config = config;
            _buildOptions = buildOptions;
            _buildScope = buildScope;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _xrefResolver = xrefResolver;
            _templateEngine = templateEngine;
            _fileLinkMapBuilder = fileLinkMapBuilder;
            _metadataProvider = metadataProvider;
        }

        public (Error? error, Document? file) ResolveContent(SourceInfo<string> href, Document referencingFile)
        {
            var (error, file, _, _, _) = TryResolveFile(referencingFile, href, true);
            if (file is null)
            {
                return default;
            }

            var origin = file.FilePath.Origin;
            if (origin == FileOrigin.Redirection || origin == FileOrigin.External)
            {
                return default;
            }

            _dependencyMapBuilder.AddDependencyItem(referencingFile.FilePath, file.FilePath, DependencyType.Include, referencingFile.ContentType);
            return (error, file);
        }

        public (Error? error, string link, Document? file) ResolveLink(SourceInfo<string> href, Document referencingFile, Document inclusionRoot)
        {
            if (href.Value.StartsWith("xref:"))
            {
                var (xrefError, resolvedHref, _, declaringFile) = _xrefResolver.ResolveXrefByHref(
                    new SourceInfo<string>(href.Value.Substring("xref:".Length), href),
                    referencingFile,
                    inclusionRoot);

                return (xrefError, resolvedHref ?? href, declaringFile);
            }

            var (error, link, fragment, linkType, file, isCrossReference) = TryResolveAbsoluteLink(href, referencingFile);

            inclusionRoot ??= referencingFile;
            if (!isCrossReference)
            {
                if (linkType == LinkType.SelfBookmark || inclusionRoot == file)
                {
                    _dependencyMapBuilder.AddDependencyItem(referencingFile.FilePath, file?.FilePath, DependencyType.File, referencingFile.ContentType);
                    _bookmarkValidator.AddBookmarkReference(referencingFile, inclusionRoot, fragment, true, href);
                }
                else if (file != null)
                {
                    _dependencyMapBuilder.AddDependencyItem(referencingFile.FilePath, file.FilePath, DependencyType.File, referencingFile.ContentType);
                    _bookmarkValidator.AddBookmarkReference(referencingFile, file, fragment, false, href);
                }
            }

            _fileLinkMapBuilder.AddFileLink(inclusionRoot.FilePath, referencingFile.FilePath, inclusionRoot.SiteUrl, link, href.Source);

            if (file != null)
            {
                link = UrlUtility.GetRelativeUrl(inclusionRoot.SiteUrl, link);
            }

            return (error, link, file);
        }

        private (Error? error, string href, string? fragment, LinkType linkType, Document? file, bool isCrossReference) TryResolveAbsoluteLink(
            SourceInfo<string> href, Document hrefRelativeTo)
        {
            var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
            var (error, file, query, fragment, linkType) = TryResolveFile(hrefRelativeTo, decodedHref);

            if (linkType == LinkType.WindowsAbsolutePath)
            {
                return (error, "", fragment, linkType, null, false);
            }

            if (linkType == LinkType.External)
            {
                if (!UrlUtility.IsValidIdnName(href))
                {
                    return (error, "", null, LinkType.RelativePath, null, false);
                }

                var resolvedHref = _config.RemoveHostName ? UrlUtility.RemoveLeadingHostName(href, _config.HostName) : href;
                return (error, resolvedHref, fragment, LinkType.AbsolutePath, null, false);
            }

            // Cannot resolve the file, leave href as is
            if (file is null)
            {
                return (error, href, fragment, linkType, null, false);
            }

            // Self reference, don't build the file, leave href as is
            if (file == hrefRelativeTo)
            {
                var selfUrl = linkType == LinkType.SelfBookmark ? "" : Path.GetFileName(file.SiteUrl);

                return (error, UrlUtility.MergeUrl(selfUrl, query, fragment), fragment, LinkType.SelfBookmark, null, false);
            }

            if (file.FilePath.Origin == FileOrigin.Redirection)
            {
                return (error, UrlUtility.MergeUrl(file.SiteUrl, query, fragment), null, linkType, file, false);
            }

            if (error is null && _buildScope.OutOfScope(file))
            {
                return (Errors.Link.LinkOutOfScope(href, file), href, fragment, linkType, null, false);
            }

            // For static hosting, reference file in fallback repo should be resolved to docs site URL
            if (file.FilePath.Origin == FileOrigin.Fallback && file.ContentType == ContentType.Page && _config.OutputUrlType != OutputUrlType.Docs)
            {
                var siteUrl = _documentProvider.GetDocsSiteUrl(file.FilePath);
                return (error, UrlUtility.MergeUrl($"https://{_config.HostName}{siteUrl}", query, fragment), fragment, linkType, file, false);
            }

            return (error, UrlUtility.MergeUrl(file.SiteUrl, query, fragment), fragment, linkType, file, false);
        }

        private (Error? error, Document? file, string? query, string? fragment, LinkType linkType) TryResolveFile(
            Document referencingFile, SourceInfo<string> href, bool lookupGitCommits = false)
        {
            href = new SourceInfo<string>(href.Value.Trim(), href.Source).Or("");
            var (path, query, fragment) = UrlUtility.SplitUrl(href);
            var linkType = UrlUtility.GetLinkType(href);
            switch (linkType)
            {
                case LinkType.SelfBookmark:
                    return (null, referencingFile, query, fragment, linkType);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.Link.LocalFilePath(href), null, null, null, linkType);

                case LinkType.RelativePath:
                    if (string.IsNullOrEmpty(path))
                    {
                        // https://tools.ietf.org/html/rfc2396#section-4.2
                        // a hack way to process empty href
                        return (null, referencingFile, query, fragment, LinkType.SelfBookmark);
                    }

                    // resolve file
                    lookupGitCommits |= _buildScope.GetContentType(path) == ContentType.Resource;
                    var file = TryResolveRelativePath(referencingFile.FilePath, path, lookupGitCommits);

                    // for LandingPage should not be used,
                    // it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(referencingFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            file = TryResolveRelativePath(referencingFile.FilePath, $"{path}.md", lookupGitCommits);
                        }

                        // Do not report error for landing page
                        return (null, file, query, fragment, linkType);
                    }

                    if (file is null)
                    {
                        return (Errors.Link.FileNotFound(
                            new SourceInfo<string>(path, href)), null, query, fragment, linkType);
                    }

                    return (null, file, query, fragment, linkType);

                default:
                    return (null, null, null, null, linkType);
            }
        }

        private Document? TryResolveRelativePath(FilePath referencingFile, string relativePath, bool lookupFallbackCommits)
        {
            FilePath path;
            FilePath? actualPath;
            PathString pathToDocset;

            if (relativePath.StartsWith("~/") || relativePath.StartsWith("~\\"))
            {
                var (_, metadata) = _metadataProvider.GetMetadata(referencingFile);
                pathToDocset = new PathString(Path.Combine(_buildOptions.DocsetPath, metadata.TildePath, relativePath.Substring(2).TrimStart('/', '\\')));
            }
            else
            {
                // Path relative to referencing file
                var baseDirectory = Path.GetDirectoryName(referencingFile.Path) ?? "";
                pathToDocset = new PathString(Path.Combine(baseDirectory, relativePath));
            }

            // the relative path could be outside docset
            if (pathToDocset.Value.StartsWith("."))
            {
                pathToDocset = new PathString(Path.GetRelativePath(_buildOptions.DocsetPath, Path.Combine(_buildOptions.DocsetPath, pathToDocset.Value)));
            }

            // resolve from the current docset for files in dependencies
            if (referencingFile.Origin == FileOrigin.Dependency)
            {
                if (!pathToDocset.StartsWithPath(referencingFile.DependencyName, out _))
                {
                    return null;
                }
                path = FilePath.Dependency(pathToDocset, referencingFile.DependencyName);
                if (_buildScope.TryGetActualFilePath(path, out actualPath))
                {
                    return _documentProvider.GetDocument(actualPath);
                }
                return null;
            }

            // resolve from redirection files
            path = FilePath.Redirection(pathToDocset);
            if (_redirectionProvider.Contains(path))
            {
                return _documentProvider.GetDocument(path);
            }

            // resolve from dependent docsets
            foreach (var (dependencyName, _) in _config.Dependencies)
            {
                if (pathToDocset.StartsWithPath(dependencyName, out _))
                {
                    path = FilePath.Dependency(pathToDocset, dependencyName);
                    if (_buildScope.TryGetActualFilePath(path, out actualPath))
                    {
                        return _documentProvider.GetDocument(actualPath);
                    }
                }
            }

            // resolve from entry docset
            path = FilePath.Content(pathToDocset);
            if (_buildScope.TryGetActualFilePath(path, out actualPath))
            {
                return _documentProvider.GetDocument(actualPath);
            }

            // resolve from fallback docset
            if (_buildOptions.IsLocalizedBuild)
            {
                path = FilePath.Fallback(pathToDocset);
                if (_buildScope.TryGetActualFilePath(path, out actualPath))
                {
                    return _documentProvider.GetDocument(actualPath);
                }

                // resolve from fallback docset git commit history
                if (lookupFallbackCommits)
                {
                    path = FilePath.Fallback(pathToDocset, isGitCommit: true);
                    if (_buildScope.TryGetActualFilePath(path, out actualPath))
                    {
                        return _documentProvider.GetDocument(actualPath);
                    }
                }
            }

            // resolve generated content docset
            path = FilePath.Generated(pathToDocset);
            if (_buildScope.TryGetActualFilePath(path, out actualPath))
            {
                return _documentProvider.GetDocument(actualPath);
            }

            return default;
        }
    }
}
