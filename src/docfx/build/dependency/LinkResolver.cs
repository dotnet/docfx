// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal class LinkResolver
    {
        private readonly Input _input;
        private readonly Docset _docset;
        private readonly Docset _fallbackDocset;
        private readonly BuildScope _buildScope;
        private readonly WorkQueue<Document> _buildQueue;
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly GitCommitProvider _gitCommitProvider;
        private readonly IReadOnlyDictionary<string, string> _resolveAlias;
        private readonly IReadOnlyDictionary<string, Docset> _dependencies;
        private readonly XrefResolver _xrefResolver;
        private readonly TemplateEngine _templateEngine;
        private readonly FileLinkMapBuilder _fileLinkMapBuilder;

        public LinkResolver(
            Docset docset,
            Docset fallbackDocset,
            Dictionary<string, Docset> dependencies,
            Input input,
            BuildScope buildScope,
            WorkQueue<Document> buildQueue,
            GitCommitProvider gitCommitProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            XrefResolver xrefResolver,
            TemplateEngine templateEngine,
            FileLinkMapBuilder fileLinkMapBuilder)
        {
            _input = input;
            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _buildScope = buildScope;
            _buildQueue = buildQueue;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefResolver = xrefResolver;
            _resolveAlias = LoadResolveAlias(docset.Config);
            _dependencies = dependencies;
            _templateEngine = templateEngine;
            _fileLinkMapBuilder = fileLinkMapBuilder;
        }

        public (Error error, string content, Document file) ResolveContent(
            SourceInfo<string> path, Document referencingFile, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, content, child) = TryResolveContent(referencingFile, path);

            _dependencyMapBuilder.AddDependencyItem(referencingFile, child, dependencyType);

            return (error, content, child);
        }

        public (Error error, string link, Document file) ResolveRelativeLink(
            Document relativeToFile, SourceInfo<string> path, Document referencingFile)
        {
            var (error, link, file) = ResolveAbsoluteLink(path, referencingFile);

            if (file != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, file);
        }

        public (Error error, string link, Document file) ResolveAbsoluteLink(SourceInfo<string> path, Document referencingFile)
        {
            var (error, link, fragment, linkType, file, isCrossReference) = TryResolveAbsoluteLink(referencingFile, path);

            if (file != null)
            {
                _buildQueue.Enqueue(file);
            }

            // NOTE: bookmark validation result depend on current inclusion stack
            var relativeToFile = (Document)InclusionContext.RootFile ?? referencingFile;
            var isSelfBookmark = linkType == LinkType.SelfBookmark || relativeToFile == file;
            if (!isCrossReference && (isSelfBookmark || file != null))
            {
                _dependencyMapBuilder.AddDependencyItem(referencingFile, file, UrlUtility.FragmentToDependencyType(fragment));
                _bookmarkValidator.AddBookmarkReference(
                    referencingFile, isSelfBookmark ? relativeToFile : file, fragment, isSelfBookmark, path);
            }

            _fileLinkMapBuilder.AddFileLink(relativeToFile, link);

            return (error, link, file);
        }

        private (Error error, string content, Document file) TryResolveContent(Document referencingFile, SourceInfo<string> href)
        {
            var (error, file, _, _, _) = TryResolveFile(referencingFile, href, inclusion: true);

            if (file?.RedirectionUrl != null)
            {
                return default;
            }

            return file != null ? (error, _input.ReadString(file.FilePath), file) : default;
        }

        private (Error error, string href, string fragment, LinkType linkType, Document file, bool isCrossReference) TryResolveAbsoluteLink(
            Document referencingFile, SourceInfo<string> href)
        {
            Debug.Assert(href != null);

            if (href.Value.StartsWith("xref:"))
            {
                var uid = new SourceInfo<string>(href.Value.Substring("xref:".Length), href);
                var (uidError, uidHref, _, declaringFile) = _xrefResolver.ResolveAbsoluteXref(uid, referencingFile);
                var xrefLinkType = declaringFile != null ? LinkType.RelativePath : LinkType.External;

                return (uidError, uidHref, null, xrefLinkType, declaringFile, true);
            }

            var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
            var (error, file, query, fragment, linkType) = TryResolveFile(referencingFile, decodedHref);

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
            if (file == referencingFile)
            {
                if (linkType == LinkType.SelfBookmark)
                {
                    return (error, UrlUtility.MergeUrl("", query, fragment), fragment, linkType, null, false);
                }

                var selfUrl = Document.PathToRelativeUrl(
                    Path.GetFileName(file.SitePath), file.ContentType, file.Mime, file.Docset.Config.Output.Json, file.IsPage);

                return (error, UrlUtility.MergeUrl(selfUrl, query, fragment), fragment, LinkType.SelfBookmark, null, false);
            }

            if (file?.RedirectionUrl != null)
            {
                return (error, UrlUtility.MergeUrl(file.SiteUrl, query, fragment), null, linkType, file, false);
            }

            if (error is null && _buildScope.OutOfScope(file))
            {
                return (Errors.LinkOutOfScope(href, file), href, fragment, linkType, null, false);
            }

            return (error, UrlUtility.MergeUrl(file.SiteUrl, query, fragment), fragment, linkType, file, false);
        }

        private (Error error, Document file, string query, string fragment, LinkType linkType) TryResolveFile(
            Document referencingFile, SourceInfo<string> href, bool inclusion = false)
        {
            href = href.Or("");
            var (path, query, fragment) = UrlUtility.SplitUrl(href);

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.SelfBookmark:
                    return (null, referencingFile, query, fragment, LinkType.SelfBookmark);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.LocalFilePath(href), null, null, null, LinkType.WindowsAbsolutePath);

                case LinkType.RelativePath:
                    if (string.IsNullOrEmpty(path))
                    {
                        // https://tools.ietf.org/html/rfc2396#section-4.2
                        // a hack way to process empty href
                        return (null, referencingFile, query, fragment, LinkType.SelfBookmark);
                    }

                    // resolve file
                    var lookupFallbackCommits = inclusion || Document.GetContentType(path) == ContentType.Resource;
                    var file = TryResolveRelativePath(referencingFile, path, lookupFallbackCommits);

                    // for LandingPage should not be used,
                    // it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(referencingFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            file = TryResolveRelativePath(referencingFile, $"{path}.md", lookupFallbackCommits);
                        }

                        // Do not report error for landing page
                        return (null, file, query, fragment, LinkType.RelativePath);
                    }

                    if (file is null)
                    {
                        return (Errors.FileNotFound(
                            new SourceInfo<string>(path, href)), null, query, fragment, LinkType.RelativePath);
                    }

                    return (null, file, query, fragment, LinkType.RelativePath);

                default:
                    return default;
            }
        }

        private Document TryResolveRelativePath(Document referencingFile, string relativePath, bool lookupFallbackCommits)
        {
            FilePath path;

            // apply resolve alias
            var pathToDocset = ApplyResolveAlias(referencingFile, relativePath);

            // use the actual file name case
            if (_buildScope.GetActualFileName(pathToDocset, out var pathActualCase))
            {
                pathToDocset = pathActualCase;
            }

            // resolve from the current docset for files in dependencies
            if (referencingFile.FilePath.Origin == FileOrigin.Dependency)
            {
                path = new FilePath(pathToDocset, referencingFile.FilePath.DependencyName);
                if (_input.Exists(path))
                {
                    return Document.Create(referencingFile.Docset, path, _input, _templateEngine);
                }
                return null;
            }

            // resolve from redirection files
            if (_buildScope.Redirections.TryGetRedirection(pathToDocset, out var redirectFile))
            {
                return redirectFile;
            }

            // resolve from dependent docsets
            foreach (var (dependencyName, dependentDocset) in _dependencies)
            {
                var (match, _, remainingPath) = PathUtility.Match(pathToDocset, dependencyName);
                if (!match)
                {
                    // the file stored in the dependent docset should start with dependency name
                    continue;
                }

                path = new FilePath(remainingPath, dependencyName);
                if (_input.Exists(path))
                {
                    return Document.Create(dependentDocset, path, _input, _templateEngine);
                }
            }

            // resolve from entry docset
            path = new FilePath(pathToDocset);
            if (_input.Exists(path))
            {
                return Document.Create(_docset, path, _input, _templateEngine);
            }

            // resolve from fallback docset
            if (_fallbackDocset != null)
            {
                path = new FilePath(pathToDocset, FileOrigin.Fallback);
                if (_input.Exists(path))
                {
                    return Document.Create(_fallbackDocset, path, _input, _templateEngine);
                }

                // resolve from fallback docset git commit history
                if (lookupFallbackCommits)
                {
                    var (repo, _, commits) = _gitCommitProvider.GetCommitHistory(_fallbackDocset, pathToDocset);
                    var commit = repo != null && commits.Count > 1 ? commits[1] : default;
                    var docsetSourceFolder = PathUtility.NormalizeFolder(Path.GetRelativePath(_docset.Repository.Path, _docset.DocsetPath));
                    path = new FilePath(string.IsNullOrEmpty(docsetSourceFolder) ? pathToDocset : PathUtility.NormalizeFile(Path.Combine(docsetSourceFolder, pathToDocset)), commit?.Sha, FileOrigin.Fallback);
                    if (_input.Exists(path))
                    {
                        return Document.Create(_fallbackDocset, path, _input, _templateEngine);
                    }
                }
            }

            return default;
        }

        private string ApplyResolveAlias(Document referencingFile, string path)
        {
            foreach (var (alias, aliasPath) in _resolveAlias)
            {
                var (match, _, remainingPath) = PathUtility.Match(path, alias);
                if (match)
                {
                    return PathUtility.NormalizeFile(aliasPath + remainingPath);
                }
            }

            return PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(referencingFile.FilePath.GetPathToOrigin()), path));
        }

        private static Dictionary<string, string> LoadResolveAlias(Config config)
        {
            var result = new Dictionary<string, string>(PathUtility.PathComparer);

            foreach (var (alias, aliasPath) in config.ResolveAlias)
            {
                result.TryAdd(alias, PathUtility.NormalizeFolder(aliasPath));
            }

            return result.Reverse().ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
