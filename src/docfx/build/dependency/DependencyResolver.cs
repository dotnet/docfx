// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal class DependencyResolver
    {
        private readonly BuildScope _buildScope;
        private readonly WorkQueue<Document> _buildQueue;
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly GitCommitProvider _gitCommitProvider;
        private readonly IReadOnlyDictionary<string, string> _resolveAlias;
        private readonly Lazy<XrefMap> _xrefMap;
        private readonly TemplateEngine _templateEngine;

        public DependencyResolver(
            Config config,
            BuildScope buildScope,
            WorkQueue<Document> buildQueue,
            GitCommitProvider gitCommitProvider,
            BookmarkValidator bookmarkValidator,
            DependencyMapBuilder dependencyMapBuilder,
            Lazy<XrefMap> xrefMap,
            TemplateEngine templateEngine)
        {
            _buildScope = buildScope;
            _buildQueue = buildQueue;
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefMap = xrefMap;
            _resolveAlias = LoadResolveAlias(config);
            _templateEngine = templateEngine;
        }

        public (Error error, string content, Document file) ResolveContent(SourceInfo<string> path, Document declaringFile, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, content, child) = TryResolveContent(declaringFile, path);

            _dependencyMapBuilder.AddDependencyItem(declaringFile, child, dependencyType);

            return (error, content, child);
        }

        public (Error error, string link, Document file) ResolveRelativeLink(Document relativeToFile, SourceInfo<string> path, Document declaringFile)
        {
            var (error, link, file) = ResolveAbsoluteLink(path, declaringFile);

            if (file != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, file);
        }

        public (Error error, string link, Document file) ResolveAbsoluteLink(SourceInfo<string> path, Document declaringFile)
        {
            var (error, link, fragment, linkType, file) = TryResolveAbsoluteLink(declaringFile, path);

            if (file != null)
            {
                _buildQueue.Enqueue(file);
            }

            // NOTE: bookmark validation result depend on current inclusion stack
            var relativeToFile = (Document)InclusionContext.RootFile ?? declaringFile;
            var isSelfBookmark = linkType == LinkType.SelfBookmark || relativeToFile == file;
            if (isSelfBookmark || file != null)
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, file, UrlUtility.FragmentToDependencyType(fragment));
                _bookmarkValidator.AddBookmarkReference(declaringFile, isSelfBookmark ? relativeToFile : file, fragment, isSelfBookmark, path);
            }

            return (error, link, file);
        }

        public (Error error, string href, string display, IXrefSpec spec) ResolveRelativeXref(Document relativeToFile, SourceInfo<string> href, Document declaringFile)
        {
            var (error, link, display, spec) = ResolveAbsoluteXref(href, declaringFile);

            if (spec?.DeclairingFile != null)
            {
                link = UrlUtility.GetRelativeUrl(relativeToFile.SiteUrl, link);
            }

            return (error, link, display, spec);
        }

        public (Error error, string href, string display, IXrefSpec spec) ResolveAbsoluteXref(SourceInfo<string> href, Document declaringFile)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);
            string moniker = null;
            NameValueCollection queries = null;
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query);
                moniker = queries?["view"];
            }
            var displayProperty = queries?["displayProperty"];

            // need to url decode uid from input content
            var (error, resolvedHref, display, xrefSpec) = _xrefMap.Value.Resolve(Uri.UnescapeDataString(uid), href, displayProperty, declaringFile, moniker);

            if (xrefSpec?.DeclairingFile != null)
            {
                _dependencyMapBuilder.AddDependencyItem(declaringFile, xrefSpec?.DeclairingFile, DependencyType.UidInclusion);
            }

            if (!string.IsNullOrEmpty(resolvedHref))
            {
                var monikerQuery = !string.IsNullOrEmpty(moniker) ? $"view={moniker}" : "";
                resolvedHref = UrlUtility.MergeUrl(resolvedHref, monikerQuery, fragment.Length == 0 ? "" : fragment.Substring(1));
            }

            return (error, resolvedHref, display, xrefSpec);
        }

        private (Error error, string content, Document file) TryResolveContent(Document declaringFile, SourceInfo<string> href)
        {
            var (error, file, _, _, _, pathToDocset) = TryResolveFile(declaringFile, href);

            if (file?.RedirectionUrl != null)
            {
                return default;
            }

            if (file is null)
            {
                var (content, fileFromHistory) = TryResolveContentFromHistory(_gitCommitProvider, declaringFile.Docset, pathToDocset);
                if (fileFromHistory != null)
                {
                    return (null, content, fileFromHistory);
                }
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        private (Error error, string href, string fragment, LinkType linkType, Document file) TryResolveAbsoluteLink(Document declaringFile, SourceInfo<string> href)
        {
            Debug.Assert(href != null);

            if (href.Value.StartsWith("xref:"))
            {
                var uid = new SourceInfo<string>(href.Value.Substring("xref:".Length), href);
                var (uidError, uidHref, _, xrefSpec) = ResolveAbsoluteXref(uid, declaringFile);
                var xrefLinkType = xrefSpec?.DeclairingFile != null ? LinkType.RelativePath : LinkType.External;

                return (uidError, uidHref, null, xrefLinkType, xrefSpec?.DeclairingFile);
            }

            var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
            var (error, file, query, fragment, linkType, pathToDocset) = TryResolveFile(declaringFile, decodedHref);

            if (linkType == LinkType.WindowsAbsolutePath)
            {
                return (error, "", fragment, linkType, null);
            }

            // Cannot resolve the file, leave href as is
            if (file is null)
            {
                file = TryResolveResourceFromHistory(_gitCommitProvider, declaringFile.Docset, pathToDocset);
                if (file is null)
                {
                    return (error, href, fragment, linkType, null);
                }

                // set file to resource got from histroy, reset the error
                error = null;
            }

            // Self reference, don't build the file, leave href as is
            if (file == declaringFile)
            {
                if (linkType == LinkType.SelfBookmark)
                {
                    return (error, query + fragment, fragment, linkType, null);
                }
                var selfUrl = Document.PathToRelativeUrl(
                    Path.GetFileName(file.SitePath), file.ContentType, file.Mime, file.Docset.Config.Output.Json, file.IsData);
                return (error, selfUrl + query + fragment, fragment, LinkType.SelfBookmark, null);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (declaringFile.Docset.DependencyDocsets.Values.Any(v => file.Docset == v))
            {
                return (Errors.LinkIsDependency(declaringFile, file, href), href, fragment, linkType, null);
            }

            if (file?.RedirectionUrl != null)
            {
                return (error, file.SiteUrl + query + fragment, null, linkType, file);
            }

            // Pages outside build scope, don't build the file, leave href as is
            if (error is null
                && (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
                && !_buildScope.Files.Contains(file))
            {
                return (Errors.LinkOutOfScope(href, file), href, fragment, linkType, null);
            }

            return (error, file.SiteUrl + query + fragment, fragment, linkType, file);
        }

        private (Error error, Document file, string query, string fragment, LinkType linkType, string pathToDocset) TryResolveFile(Document declaringFile, SourceInfo<string> href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return default;
            }

            var (path, query, fragment) = UrlUtility.SplitUrl(href);

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.SelfBookmark:
                    return (null, declaringFile, query, fragment, LinkType.SelfBookmark, null);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.LocalFilePath(declaringFile, path), null, null, null, LinkType.WindowsAbsolutePath, null);

                case LinkType.RelativePath:
                    // Resolve path relative to docset
                    var pathToDocset = ResolveToDocsetRelativePath(path, declaringFile);

                    // Use the actual file name case
                    if (_buildScope.GetActualFileName(pathToDocset, out var pathActualCase))
                    {
                        pathToDocset = pathActualCase;
                    }

                    // resolve from redirection files
                    if (_buildScope.Redirections.TryGetRedirection(pathToDocset, out var redirectFile))
                    {
                        return (null, redirectFile, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    var file = Document.CreateFromFile(declaringFile.Docset, pathToDocset, _templateEngine);

                    // for LandingPage should not be used, it is a hack to handle some specific logic for landing page based on the user input for now
                    // which needs to be removed once the user input is correct
                    if (_templateEngine != null && TemplateEngine.IsLandingData(declaringFile.Mime))
                    {
                        if (file is null)
                        {
                            // try to resolve with .md for landing page
                            pathToDocset = ResolveToDocsetRelativePath($"{path}.md", declaringFile);
                            file = Document.CreateFromFile(declaringFile.Docset, pathToDocset, _templateEngine);
                        }

                        // Do not report error for landing page
                        return (null, file, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    if (file is null)
                    {
                        return (Errors.FileNotFound(new SourceInfo<string>(path, href)), null, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    return (null, file, query, fragment, LinkType.RelativePath, pathToDocset);

                default:
                    return default;
            }
        }

        private string ResolveToDocsetRelativePath(string path, Document declaringFile)
        {
            var docsetRelativePath = PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(declaringFile.FilePath), path));
            if (!File.Exists(Path.Combine(declaringFile.Docset.DocsetPath, docsetRelativePath)))
            {
                foreach (var (alias, aliasPath) in _resolveAlias)
                {
                    if (path.StartsWith(alias, PathUtility.PathComparison))
                    {
                        return PathUtility.NormalizeFile(Path.Combine(aliasPath, path.Substring(alias.Length)));
                    }
                }
            }
            return docsetRelativePath;
        }

        private static Document TryResolveResourceFromHistory(GitCommitProvider gitCommitProvider, Docset docset, string pathToDocset)
        {
            if (string.IsNullOrEmpty(pathToDocset))
            {
                return default;
            }

            // try to resolve from source repo's git history
            var fallbackDocset = GetFallbackDocset(docset);
            if (fallbackDocset != null && Document.GetContentType(pathToDocset) == ContentType.Resource)
            {
                var (repo, pathToRepo, commits) = gitCommitProvider.GetCommitHistory(fallbackDocset, pathToDocset);
                if (repo != null && commits.Count > 0)
                {
                    return Document.Create(fallbackDocset, pathToDocset, isFromHistory: true);
                }
            }

            return default;
        }

        private static (string content, Document file) TryResolveContentFromHistory(GitCommitProvider gitCommitProvider, Docset docset, string pathToDocset)
        {
            if (string.IsNullOrEmpty(pathToDocset))
            {
                return default;
            }

            // try to resolve from source repo's git history
            var fallbackDocset = GetFallbackDocset(docset);
            if (fallbackDocset != null)
            {
                var (repo, pathToRepo, commits) = gitCommitProvider.GetCommitHistory(fallbackDocset, pathToDocset);
                if (repo != null)
                {
                    var repoPath = PathUtility.NormalizeFolder(repo.Path);
                    if (commits.Count > 1)
                    {
                        // the latest commit would be deleting it from repo
                        if (GitUtility.TryGetContentFromHistory(repoPath, pathToRepo, commits[1].Sha, out var content))
                        {
                            return (content, Document.Create(fallbackDocset, pathToDocset, isFromHistory: true));
                        }
                    }
                }
            }

            return default;
        }

        private static Docset GetFallbackDocset(Docset docset)
        {
            if (docset.LocalizationDocset != null)
            {
                // source docset in loc build
                return docset;
            }

            if (docset.FallbackDocset != null)
            {
                // localized docset in loc build
                return docset.FallbackDocset;
            }

            // source docset in source build
            return null;
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
    }
}
