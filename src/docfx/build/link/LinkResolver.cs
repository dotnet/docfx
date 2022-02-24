// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

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
    private readonly ContentValidator _contentValidator;

    private readonly Scoped<ConcurrentHashSet<FilePath>> _additionalResources = new();

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
        MetadataProvider metadataProvider,
        ContentValidator contentValidator)
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
        _contentValidator = contentValidator;
    }

    public (Error? error, FilePath? file) ResolveContent(
        SourceInfo<string> href,
        FilePath referencingFile,
        FilePath inclusionRoot,
        bool contentFallback = true,
        bool? transitive = null)
    {
        var (error, file, _, _, _) = TryResolveFile(inclusionRoot, referencingFile, href, contentFallback, true);
        if (file is null)
        {
            return default;
        }

        var origin = file.Origin;
        if (origin == FileOrigin.Redirection || origin == FileOrigin.External)
        {
            return default;
        }

        _dependencyMapBuilder.AddDependencyItem(referencingFile, file, DependencyType.Include, transitive ?? true);
        return (error, file);
    }

    public (List<Error> errors, string link, FilePath? file) ResolveLink(
        SourceInfo<string> href, FilePath referencingFile, FilePath inclusionRoot, LinkNode? linkNode = null, string tagName = "a")
    {
        var errors = new List<Error>();
        if (href.Value.StartsWith("xref:"))
        {
            var (xrefError, xrefLink) = _xrefResolver.ResolveXrefByHref(
                new SourceInfo<string>(href.Value["xref:".Length..], href),
                referencingFile,
                inclusionRoot);

            errors.AddIfNotNull(xrefError);
            return (errors, xrefLink.Href ?? "", xrefLink.DeclaringFile);
        }

        var (error, link, fragment, linkType, file, isCrossReference) = TryResolveAbsoluteLink(
            href,
            referencingFile,
            inclusionRoot,
            linkNode,
            tagName);

        inclusionRoot ??= referencingFile;
        if (!isCrossReference)
        {
            if (linkType == LinkType.SelfBookmark || inclusionRoot == file)
            {
                _dependencyMapBuilder.AddDependencyItem(referencingFile, file, DependencyType.File);
                _bookmarkValidator.AddBookmarkReference(referencingFile, inclusionRoot, fragment, true, href);
            }
            else if (file != null)
            {
                _dependencyMapBuilder.AddDependencyItem(referencingFile, file, DependencyType.File);
                _bookmarkValidator.AddBookmarkReference(referencingFile, file, fragment, false, href);
            }
        }

        if (!string.IsNullOrEmpty(link))
        {
            _fileLinkMapBuilder.AddFileLink(inclusionRoot, referencingFile, link, href.Source);
        }

        if (file != null && !JsonSchemaProvider.OutputAbsoluteUrl(_documentProvider.GetMime(inclusionRoot)))
        {
            link = UrlUtility.GetRelativeUrl(_documentProvider.GetSiteUrl(inclusionRoot), link);
        }

        return (error, link, file);
    }

    public IEnumerable<FilePath> GetAdditionalResources() => _additionalResources.Value;

    private (List<Error> error, string href, string? fragment, LinkType linkType, FilePath? file, bool isCrossReference) TryResolveAbsoluteLink(
        SourceInfo<string> href, FilePath hrefRelativeTo, FilePath inclusionRoot, LinkNode? linkNode, string tagName)
    {
        var errors = new List<Error>();
        var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
        var (error, file, query, fragment, linkType) = TryResolveFile(inclusionRoot, hrefRelativeTo, decodedHref);
        errors.AddIfNotNull(error);

        if (linkType == LinkType.WindowsAbsolutePath)
        {
            return (errors, "", fragment, linkType, null, false);
        }

        ValidateLink(inclusionRoot, linkNode);
        if (linkType == LinkType.External)
        {
            if (_config.TrustedDomains.TryGetValue(tagName, out var domains) && !domains.IsTrusted(href, out var untrustedDomain))
            {
                if (tagName == "img")
                {
                    errors.Add(Errors.Content.ExternalImage(new(hrefRelativeTo), href, tagName, untrustedDomain));
                }
                else
                {
                    errors.Add(Errors.Content.DisallowedDomain(new(hrefRelativeTo), href, tagName, untrustedDomain));
                }
                return (errors, "", fragment, LinkType.AbsolutePath, null, false);
            }

            var resolvedHref = _config.RemoveHostName ? UrlUtility.RemoveLeadingHostName(href, _config.HostName) : href;
            if (href != resolvedHref)
            {
                linkType = LinkType.AbsolutePath;
            }

            return (errors, resolvedHref, fragment, linkType, null, false);
        }

        // Cannot resolve the file, leave href as is
        if (file is null)
        {
            return (errors, href, fragment, linkType, null, false);
        }

        var siteUrl = _documentProvider.GetSiteUrl(file);

        // Self reference, don't build the file, leave href as is
        if (file == inclusionRoot)
        {
            var selfUrl = linkType == LinkType.SelfBookmark ? "" : Path.GetFileName(siteUrl);

            return (errors, UrlUtility.MergeUrl(selfUrl, query, fragment), fragment, LinkType.SelfBookmark, null, false);
        }

        if (file.Origin == FileOrigin.Redirection)
        {
            return (errors, UrlUtility.MergeUrl(siteUrl, query, fragment), null, linkType, file, false);
        }

        if (error is null && _buildScope.OutOfScope(file))
        {
            if (file.Origin == FileOrigin.Dependency && _buildScope.GetContentType(file) == ContentType.Resource)
            {
                Watcher.Write(() => _additionalResources.Value.TryAdd(file));
                return (errors, UrlUtility.MergeUrl(siteUrl, query, fragment), null, linkType, file, false);
            }
            errors.Add(Errors.Link.LinkOutOfScope(href, file));
            return (errors, href, fragment, linkType, null, false);
        }

        if (file.Origin == FileOrigin.Fallback && _config.UrlType != UrlType.Docs &&
            _documentProvider.GetContentType(file) == ContentType.Page)
        {
#pragma warning disable CS0618 // Docs pdf build uses static url, but links in fallback repo should be resolved to docs site URL
            var docsSiteUrl = _documentProvider.GetDocsSiteUrl(file);
#pragma warning restore CS0618
            return (errors, UrlUtility.MergeUrl($"https://{_config.HostName}{docsSiteUrl}", query, fragment), fragment, linkType, file, false);
        }

        return (errors, UrlUtility.MergeUrl(siteUrl, query, fragment), fragment, linkType, file, false);
    }

    private (Error? error, FilePath? file, string? query, string? fragment, LinkType linkType) TryResolveFile(
        FilePath inclusionRoot, FilePath referencingFile, SourceInfo<string> href, bool contentFallback = true, bool lookupGitCommits = false)
    {
        href = new SourceInfo<string>(href.Value.Trim(), href.Source).Or("");
        var (path, query, fragment) = UrlUtility.SplitUrl(href);
        var linkType = UrlUtility.GetLinkType(href);
        switch (linkType)
        {
            case LinkType.SelfBookmark:
                return (null, inclusionRoot, query, fragment, linkType);

            case LinkType.WindowsAbsolutePath:
                return (Errors.Link.LocalFilePath(href), null, null, null, linkType);

            case LinkType.RelativePath:
                if (string.IsNullOrEmpty(path))
                {
                    // https://tools.ietf.org/html/rfc2396#section-4.2
                    // a hack way to process empty href
                    return (null, inclusionRoot, query, fragment, LinkType.SelfBookmark);
                }

                // resolve file
                lookupGitCommits |= _buildScope.GetContentType(path) == ContentType.Resource;
                var file = TryResolveRelativePath(referencingFile, path, lookupGitCommits, contentFallback);

                // for LandingPage should not be used,
                // it is a hack to handle some specific logic for landing page based on the user input for now
                // which needs to be removed once the user input is correct
                if (_templateEngine != null && JsonSchemaProvider.IsLandingData(_documentProvider.GetMime(inclusionRoot)))
                {
                    if (file is null)
                    {
                        // try to resolve with .md for landing page
                        file = TryResolveRelativePath(referencingFile, $"{path}.md", lookupGitCommits, contentFallback);
                    }

                    // Do not report error for landing page
                    return (null, file, query, fragment, linkType);
                }

                if (file is null)
                {
                    return (Errors.Link.FileNotFound(new SourceInfo<string>(path, href)), null, query, fragment, linkType);
                }

                return (null, file, query, fragment, linkType);

            default:
                return (null, null, null, null, linkType);
        }
    }

    private FilePath? TryResolveRelativePath(FilePath referencingFile, string relativePath, bool lookupFallbackCommits, bool contentFallback)
    {
        FilePath? actualPath;
        PathString pathToDocset;

        if (relativePath.StartsWith("~/") || relativePath.StartsWith("~\\"))
        {
            var metadata = _metadataProvider.GetMetadata(ErrorBuilder.Null, referencingFile);
            pathToDocset = new PathString(Path.Combine(metadata.TildePath, relativePath[2..].TrimStart('/', '\\')));
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
            if (_buildScope.TryGetActualFilePath(FilePath.Dependency(pathToDocset, referencingFile.DependencyName), out actualPath))
            {
                return actualPath;
            }
            return null;
        }

        // resolve from redirection files
        if (_redirectionProvider.TryGetValue(pathToDocset, out actualPath))
        {
            return actualPath;
        }

        // resolve from dependent docsets
        foreach (var (dependencyName, _) in _config.Dependencies)
        {
            if (pathToDocset.StartsWithPath(dependencyName, out _))
            {
                if (_buildScope.TryGetActualFilePath(FilePath.Dependency(pathToDocset, dependencyName), out actualPath))
                {
                    return actualPath;
                }
            }
        }

        // resolve from entry docset
        if (_buildScope.TryGetActualFilePath(FilePath.Content(pathToDocset), out actualPath))
        {
            return actualPath;
        }

        // resolve from fallback docset
        if (_buildOptions.IsLocalizedBuild && (referencingFile.Origin == FileOrigin.Fallback || contentFallback))
        {
            if (_buildScope.TryGetActualFilePath(FilePath.Fallback(pathToDocset), out actualPath))
            {
                return actualPath;
            }

            // resolve from fallback docset git commit history
            if (lookupFallbackCommits)
            {
                if (_buildScope.TryGetActualFilePath(FilePath.Fallback(pathToDocset, isGitCommit: true), out actualPath))
                {
                    return actualPath;
                }
            }
        }

        // resolve generated content docset
        if (_buildScope.TryGetActualFilePath(FilePath.Generated(pathToDocset), out actualPath))
        {
            return actualPath;
        }

        return default;
    }

    private void ValidateLink(FilePath file, LinkNode? node)
    {
        if (node is null)
        {
            return;
        }

        _contentValidator.ValidateLink(file, node);
    }
}
