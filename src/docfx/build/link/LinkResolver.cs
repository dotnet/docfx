// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LinkResolver
    {
        private readonly Config _config;
        private readonly Input _input;
        private readonly BuildOptions _buildOptions;
        private readonly SourceMap _sourceMap;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly WorkQueue<FilePath> _buildQueue;
        private readonly DocumentProvider _documentProvider;
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly XrefResolver _xrefResolver;
        private readonly TemplateEngine _templateEngine;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;

        public LinkResolver(
            Config config,
            Input input,
            BuildOptions buildOptions,
            SourceMap sourceMap,
            BuildScope buildScope,
            WorkQueue<FilePath> buildQueue,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            XrefResolver xrefResolver,
            TemplateEngine templateEngine,
            FileLinkMapBuilder fileLinkMapBuilder)
        {
            _config = config;
            _input = input;
            _buildOptions = buildOptions;
            _sourceMap = sourceMap;
            _buildScope = buildScope;
            _buildQueue = buildQueue;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _xrefResolver = xrefResolver;
            _templateEngine = templateEngine;
            _fileLinkMapBuilder = fileLinkMapBuilder;
        }

        public (Error? error, string? content, Document? file) ResolveContent(
            SourceInfo<string> href, Document referencingFile, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, file, _, _, _) = TryResolveFile(referencingFile, href, inclusion: true);

            if (file is null || file.ContentType == ContentType.Redirection)
            {
                return default;
            }

            var content = _input.ReadString(file.FilePath);

            _dependencyMapBuilder.AddDependencyItem(referencingFile, file, dependencyType);

            return (error, content, file);
        }

        public (Error? error, string link, Document? file) ResolveLink(
            SourceInfo<string> href, Document hrefRelativeTo, Document inclusionRoot)
        {
            if (href.Value.StartsWith("xref:"))
            {
                var uid = new SourceInfo<string>(href.Value.Substring("xref:".Length), href);
                var (uidError, uidHref, _, declaringFile) = _xrefResolver.ResolveXref(uid, hrefRelativeTo, inclusionRoot);

                return (uidError, uidHref ?? href, declaringFile);
            }

            var (error, link, fragment, linkType, file, isCrossReference) = TryResolveAbsoluteLink(href, hrefRelativeTo);

            if (file != null)
            {
                _buildQueue.Enqueue(file.FilePath);
            }

            inclusionRoot ??= hrefRelativeTo;
            if (!isCrossReference)
            {
                if (linkType == LinkType.SelfBookmark || inclusionRoot == file)
                {
                    _dependencyMapBuilder.AddDependencyItem(hrefRelativeTo, file, UrlUtility.FragmentToDependencyType(fragment));
                    _bookmarkValidator.AddBookmarkReference(hrefRelativeTo, inclusionRoot, fragment, true, href);
                }
                else if (file != null)
                {
                    _dependencyMapBuilder.AddDependencyItem(hrefRelativeTo, file, UrlUtility.FragmentToDependencyType(fragment));
                    _bookmarkValidator.AddBookmarkReference(hrefRelativeTo, file, fragment, false, href);
                }
            }

            _fileLinkMapBuilder.AddFileLink(inclusionRoot.FilePath, inclusionRoot.SiteUrl, link);

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

            return (error, UrlUtility.MergeUrl(file.SiteUrl, query, fragment), fragment, linkType, file, false);
        }

        private (Error? error, Document? file, string? query, string? fragment, LinkType linkType) TryResolveFile(
            Document referencingFile, SourceInfo<string> href, bool inclusion = false)
        {
            href = new SourceInfo<string>(href.Value.Trim(), href.Source).Or("");
            var (path, query, fragment) = UrlUtility.SplitUrl(href);

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.SelfBookmark:
                    return (null, referencingFile, query, fragment, LinkType.SelfBookmark);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.Link.LocalFilePath(href), null, null, null, LinkType.WindowsAbsolutePath);

                case LinkType.RelativePath:
                    if (string.IsNullOrEmpty(path))
                    {
                        // https://tools.ietf.org/html/rfc2396#section-4.2
                        // a hack way to process empty href
                        return (null, referencingFile, query, fragment, LinkType.SelfBookmark);
                    }

                    // resolve file
                    var lookupFallbackCommits = inclusion || _buildScope.GetContentType(path) == ContentType.Resource;
                    var file = TryResolveRelativePath(referencingFile.FilePath, path, lookupFallbackCommits);

                    // for LandingPage should not be used,
                    // it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(referencingFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            file = TryResolveRelativePath(referencingFile.FilePath, $"{path}.md", lookupFallbackCommits);
                        }

                        // Do not report error for landing page
                        return (null, file, query, fragment, LinkType.RelativePath);
                    }

                    if (file is null)
                    {
                        return (Errors.Config.FileNotFound(
                            new SourceInfo<string>(path, href)), null, query, fragment, LinkType.RelativePath);
                    }

                    return (null, file, query, fragment, LinkType.RelativePath);

                default:
                    return default;
            }
        }

        private Document? TryResolveRelativePath(FilePath referencingFile, string relativePath, bool lookupFallbackCommits)
        {
            FilePath path;
            PathString pathToDocset;

            if (relativePath.StartsWith("~/") || relativePath.StartsWith("~\\"))
            {
                // Treat ~/ as path relative to docset
                pathToDocset = new PathString(relativePath.Substring(2).TrimStart('/', '\\'));
            }
            else
            {
                // Path relative to referencing file
                var baseDirectory = Path.GetDirectoryName(referencingFile.Path) ?? "";
                pathToDocset = new PathString(Path.Combine(baseDirectory, relativePath));

                // the relative path could be outside docset
                if (pathToDocset.Value.StartsWith("."))
                {
                    pathToDocset = new PathString(Path.GetRelativePath(_buildOptions.DocsetPath, Path.Combine(_buildOptions.DocsetPath, pathToDocset.Value)));
                }
            }

            // use the actual file name case
            if (_buildScope.GetActualFileName(pathToDocset, out var pathActualCase))
            {
                pathToDocset = pathActualCase;
            }

            // resolve from the current docset for files in dependencies
            if (referencingFile.Origin == FileOrigin.Dependency)
            {
                if (!pathToDocset.StartsWithPath(referencingFile.DependencyName, out _))
                {
                    return null;
                }
                path = FilePath.Dependency(pathToDocset, referencingFile.DependencyName);
                if (_input.Exists(path))
                {
                    return _documentProvider.GetDocument(path);
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
                    if (_input.Exists(path))
                    {
                        return _documentProvider.GetDocument(path);
                    }
                }
            }

            // resolve from entry docset
            path = FilePath.Content(pathToDocset, _sourceMap.GetOriginalFilePath(pathToDocset));
            if (_input.Exists(path))
            {
                return _documentProvider.GetDocument(path);
            }

            // resolve from fallback docset
            if (_buildOptions.IsLocalizedBuild)
            {
                path = FilePath.Fallback(pathToDocset);
                if (_input.Exists(path))
                {
                    return _documentProvider.GetDocument(path);
                }

                // resolve from fallback docset git commit history
                if (lookupFallbackCommits)
                {
                    path = FilePath.Fallback(pathToDocset, isGitCommit: true);
                    if (_input.Exists(path))
                    {
                        return _documentProvider.GetDocument(path);
                    }
                }
            }

            return default;
        }
    }
}
