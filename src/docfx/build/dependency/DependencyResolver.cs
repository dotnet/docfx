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
    internal class DependencyResolver
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
        private readonly Lazy<XrefMap> _xrefMap;
        private readonly TemplateEngine _templateEngine;

        public DependencyResolver(
            Docset docset,
            Docset fallbackDocset,
            Input input,
            BuildScope buildScope,
            WorkQueue<Document> buildQueue,
            GitCommitProvider gitCommitProvider,
            BookmarkValidator bookmarkValidator,
            RestoreGitMap restoreGitMap,
            DependencyMapBuilder dependencyMapBuilder,
            Lazy<XrefMap> xrefMap,
            TemplateEngine templateEngine)
        {
            _input = input;
            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _buildScope = buildScope;
            _buildQueue = buildQueue;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefMap = xrefMap;
            _resolveAlias = LoadResolveAlias(docset.Config);
            _dependencies = LoadDependencies(docset, restoreGitMap);
            _templateEngine = templateEngine;
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

            return (error, link, file);
        }

        public (Error error, string href, string display, Document declaringFile) ResolveRelativeXref(
            Document relativeToFile, SourceInfo<string> href, Document referencingFile)
        {
            var (error, link, display, declaringFile) = ResolveAbsoluteXref(href, referencingFile);

            if (declaringFile != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, display, declaringFile);
        }

        public (Error error, string href, string display, Document declaringFile) ResolveAbsoluteXref(
            SourceInfo<string> href, Document referencingFile)
            => _xrefMap.Value.ResolveToLink(href, referencingFile);

        private (Error error, string content, Document file) TryResolveContent(Document referencingFile, SourceInfo<string> href)
        {
            var (error, file, _, _, _) = TryResolveFile(referencingFile, href, inclusion: true);

            if (file?.RedirectionUrl != null)
            {
                return default;
            }

            return file != null ? (error, _input.ReadText(file.FilePath), file) : default;
        }

        private (Error error, string href, string fragment, LinkType linkType, Document file, bool isCrossReference) TryResolveAbsoluteLink(
            Document referencingFile, SourceInfo<string> href)
        {
            Debug.Assert(href != null);

            if (href.Value.StartsWith("xref:"))
            {
                var uid = new SourceInfo<string>(href.Value.Substring("xref:".Length), href);
                var (uidError, uidHref, _, declaringFile) = ResolveAbsoluteXref(uid, referencingFile);
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
                    return (error, query + fragment, fragment, linkType, null, false);
                }

                var selfUrl = Document.PathToRelativeUrl(
                    Path.GetFileName(file.SitePath), file.ContentType, file.Mime, file.Docset.Config.Output.Json, file.IsPage);

                return (error, selfUrl + query + fragment, fragment, LinkType.SelfBookmark, null, false);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (file.FilePath.Origin == FileOrigin.Dependency)
            {
                return (Errors.LinkIsDependency(href, file), href, fragment, linkType, null, false);
            }

            if (file?.RedirectionUrl != null)
            {
                return (error, file.SiteUrl + query + fragment, null, linkType, file, false);
            }

            // Pages outside build scope, don't build the file, leave href as is
            if (error is null
                && (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
                && !_buildScope.Files.Contains(file))
            {
                return (Errors.LinkOutOfScope(href, file), href, fragment, linkType, null, false);
            }

            return (error, file.SiteUrl + query + fragment, fragment, linkType, file, false);
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

                    // Apply resolve alias
                    var pathToDocset = ApplyResolveAlias(path, referencingFile);

                    // Use the actual file name case
                    if (_buildScope.GetActualFileName(pathToDocset, out var pathActualCase))
                    {
                        pathToDocset = pathActualCase;
                    }

                    // resolve from redirection files
                    if (_buildScope.Redirections.TryGetRedirection(pathToDocset, out var redirectFile))
                    {
                        return (null, redirectFile, query, fragment, LinkType.RelativePath);
                    }

                    // resolve file
                    var lookupFallbackCommits = inclusion || Document.GetContentType(pathToDocset) == ContentType.Resource;
                    var file = TryResolveFile(referencingFile, pathToDocset, lookupFallbackCommits);

                    // for LandingPage should not be used,
                    // it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(referencingFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            pathToDocset = ApplyResolveAlias($"{path}.md", referencingFile);
                            file = TryResolveFile(referencingFile, pathToDocset, lookupFallbackCommits);
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

        private string ApplyResolveAlias(string path, Document referencingFile)
        {
            var docsetRelativePath = PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(referencingFile.FilePath.Path), path));
            if (!File.Exists(Path.Combine(referencingFile.Docset.DocsetPath, docsetRelativePath)))
            {
                foreach (var (alias, aliasPath) in _resolveAlias)
                {
                    if (path.StartsWith(alias, PathUtility.PathComparison))
                    {
                        return PathUtility.NormalizeFile(aliasPath + path.Substring(alias.Length));
                    }
                }
            }
            return docsetRelativePath;
        }

        private Document TryResolveFile(Document declaringFile, string pathToDocset, bool lookupFallbackCommits)
        {
            // resolve from the current docset for files in dependencies
            if (declaringFile.FilePath.Origin == FileOrigin.Dependency)
            {
                if (File.Exists(Path.Combine(declaringFile.Docset.DocsetPath, pathToDocset)))
                {
                    var path = new FilePath(pathToDocset, declaringFile.FilePath.DependencyName);
                    return Document.Create(declaringFile.Docset, path, _templateEngine);
                }
                return null;
            }

            // resolve from dependencies
            foreach (var (dependencyName, dependencyDocset) in _dependencies)
            {
                var (match, _, remainingPath) = PathUtility.Match(pathToDocset, dependencyName);
                if (!match)
                {
                    // the file stored in the dependent docset should start with dependency name
                    continue;
                }

                if (File.Exists(Path.Combine(dependencyDocset.DocsetPath, remainingPath)))
                {
                    return Document.Create(dependencyDocset, new FilePath(remainingPath, dependencyName), _templateEngine);
                }
            }

            // resolve from entry docset
            if (File.Exists(Path.Combine(_docset.DocsetPath, pathToDocset)))
            {
                return Document.Create(_docset, new FilePath(pathToDocset), _templateEngine);
            }

            // resolve from fallback docset
            if (_fallbackDocset != null)
            {
                if (File.Exists(Path.Combine(_fallbackDocset.DocsetPath, pathToDocset)))
                {
                    return Document.Create(_fallbackDocset, new FilePath(pathToDocset, FileOrigin.Fallback), _templateEngine);
                }

                // resolve from fallback docset git commit history
                if (lookupFallbackCommits)
                {
                    var (repo, _, commits) = _gitCommitProvider.GetCommitHistory(_fallbackDocset, pathToDocset);
                    if (repo != null && commits.Length > 1)
                    {
                        return Document.Create(
                            _fallbackDocset, new FilePath(pathToDocset, commits[1].Sha, FileOrigin.Fallback), _templateEngine);
                    }
                }
            }

            return default;
        }

        private static Dictionary<string, string> LoadResolveAlias(Config config)
        {
            var result = new Dictionary<string, string>(PathUtility.PathComparer);

            foreach (var (alias, aliasPath) in config.ResolveAlias)
            {
                result.TryAdd(PathUtility.NormalizeFolder(alias), PathUtility.NormalizeFolder(aliasPath));
            }

            return result.Reverse().ToDictionary(item => item.Key, item => item.Value);
        }

        private static Dictionary<string, Docset> LoadDependencies(Docset docset, RestoreGitMap restoreGitMap)
        {
            var config = docset.Config;
            var result = new Dictionary<string, Docset>(config.Dependencies.Count, PathUtility.PathComparer);
            foreach (var (name, dependency) in config.Dependencies)
            {
                var (dir, _) = restoreGitMap.GetGitRestorePath(dependency, docset.DocsetPath);

                result.TryAdd(name, new Docset(dir, docset.Locale, config));
            }
            return result;
        }
    }
}
