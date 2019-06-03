// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace Microsoft.Docs.Build
{
    internal class DependencyResolver
    {
        private readonly BookmarkValidator _bookmarkValidator;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly GitCommitProvider _gitCommitProvider;
        private readonly Lazy<XrefMap> _xrefMap;
        private readonly bool _forLandingPage = false;

        public DependencyResolver(GitCommitProvider gitCommitProvider, BookmarkValidator bookmarkValidator, DependencyMapBuilder dependencyMapBuilder, Lazy<XrefMap> xrefMap, bool forLandingPage = false)
        {
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefMap = xrefMap;

            // forLandingPage should not be used, it is a hack to handle some specific logic for landing page based on the user input for now
            // which needs to be removed once the user input is correct
            _forLandingPage = forLandingPage;
        }

        public (Error error, string content, Document file) ResolveContent(SourceInfo<string> path, Document relativeTo, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, content, child) = TryResolveContent(relativeTo, path);

            _dependencyMapBuilder.AddDependencyItem(relativeTo, child, dependencyType);

            return (error, content, child);
        }

        public (Error error, string link, Document file) ResolveLink(SourceInfo<string> path, Document relativeTo, Document resultRelativeTo, Action<Document> buildChild)
        {
            var (error, link, fragment, linkType, file) = TryResolveHref(relativeTo, path, resultRelativeTo);

            if (file != null && buildChild != null)
            {
                buildChild(file);
            }

            var isSelfBookmark = linkType == LinkType.SelfBookmark || resultRelativeTo == file;
            if (isSelfBookmark || file != null)
            {
                _dependencyMapBuilder.AddDependencyItem(relativeTo, file, UrlUtility.FragmentToDependencyType(fragment));
                _bookmarkValidator.AddBookmarkReference(relativeTo, isSelfBookmark ? resultRelativeTo : file, fragment, isSelfBookmark, path);
            }

            return (error, link, file);
        }

        public (Error error, string href, string display, Document file) ResolveXref(SourceInfo<string> href, Document relativeTo, Document rootFile)
        {
            var (uid, query, fragment) = UrlUtility.SplitUrl(href);
            string moniker = null;
            NameValueCollection queries = null;
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query.Substring(1));
                moniker = queries?["view"];
            }
            var displayProperty = queries?["displayProperty"];

            // need to url decode uid from input content
            var (error, resolvedHref, display, referencedFile) = _xrefMap.Value.Resolve(Uri.UnescapeDataString(uid), href, displayProperty, relativeTo, rootFile, moniker);

            if (referencedFile != null)
            {
                _dependencyMapBuilder.AddDependencyItem(rootFile, referencedFile, DependencyType.UidInclusion);
            }

            if (!string.IsNullOrEmpty(resolvedHref))
            {
                var monikerQuery = !string.IsNullOrEmpty(moniker) ? $"view={moniker}" : "";
                resolvedHref = UrlUtility.MergeUrl(resolvedHref, monikerQuery, fragment.Length == 0 ? "" : fragment.Substring(1));
            }
            return (error, resolvedHref, display, referencedFile);
        }

        /// <summary>
        /// Get relative url from file to the file relative to
        /// </summary>
        public string GetRelativeUrl(Document fileRelativeTo, Document file)
        {
            var relativePath = PathUtility.GetRelativePathToFile(fileRelativeTo.SitePath, file.SitePath);
            return Document.PathToRelativeUrl(relativePath, file.ContentType, file.Schema, file.Docset.Config.Output.Json);
        }

        private (Error error, string content, Document file) TryResolveContent(Document relativeTo, SourceInfo<string> href)
        {
            var (error, file, _, _, _, pathToDocset) = TryResolveFile(relativeTo, href);

            if (file?.RedirectionUrl != null)
            {
                return default;
            }

            if (file is null)
            {
                var (content, fileFromHistory) = TryResolveContentFromHistory(_gitCommitProvider, relativeTo.Docset, pathToDocset);
                if (fileFromHistory != null)
                {
                    return (null, content, fileFromHistory);
                }
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        private (Error error, string href, string fragment, LinkType? linkType, Document file) TryResolveHref(Document relativeTo, SourceInfo<string> href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);
            Debug.Assert(href != null);

            if (href.Value.StartsWith("xref:") != false)
            {
                href.Value = href.Value.Substring("xref:".Length);
                var (uidError, uidHref, _, referencedFile) = ResolveXref(href, relativeTo, resultRelativeTo);
                return (uidError, uidHref, null, null, referencedFile);
            }

            var decodedHref = new SourceInfo<string>(Uri.UnescapeDataString(href), href);
            var (error, file, query, fragment, linkType, pathToDocset) = TryResolveFile(relativeTo, decodedHref);

            if (linkType == LinkType.WindowsAbsolutePath)
            {
                return (error, "", fragment, linkType, null);
            }

            // Cannot resolve the file, leave href as is
            if (file is null)
            {
                file = TryResolveResourceFromHistory(_gitCommitProvider, relativeTo.Docset, pathToDocset);
                if (file is null)
                {
                    return (error, href, fragment, linkType, null);
                }

                // set file to resource got from histroy, reset the error
                error = null;
            }

            // Self reference, don't build the file, leave href as is
            if (file == relativeTo)
            {
                if (linkType == LinkType.SelfBookmark)
                {
                    return (error, query + fragment, fragment, linkType, null);
                }
                var selfUrl = Document.PathToRelativeUrl(
                    Path.GetFileName(file.SitePath), file.ContentType, file.Schema, file.Docset.Config.Output.Json);
                return (error, selfUrl + query + fragment, fragment, LinkType.SelfBookmark, null);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (relativeTo.Docset.DependencyDocsets.Values.Any(v => file.Docset == v))
            {
                return (Errors.LinkIsDependency(relativeTo, file, href), href, fragment, linkType, null);
            }

            // Make result relative to `resultRelativeTo`
            var relativeUrl = GetRelativeUrl(resultRelativeTo, file);

            if (file?.RedirectionUrl != null)
            {
                return (error, relativeUrl + query + fragment, null, linkType, null);
            }

            // Pages outside build scope, don't build the file, use relative href
            if (error is null
                && (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
                && !file.Docset.BuildScope.Contains(file))
            {
                return (Errors.LinkOutOfScope(href, file), relativeUrl + query + fragment, fragment, linkType, null);
            }

            return (error, relativeUrl + query + fragment, fragment, linkType, file);
        }

        private (Error error, Document file, string query, string fragment, LinkType? linkType, string pathToDocset) TryResolveFile(Document relativeTo, SourceInfo<string> href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null, null, null);
            }

            var (path, query, fragment) = UrlUtility.SplitUrl(href);

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.SelfBookmark:
                    return (null, relativeTo, query, fragment, LinkType.SelfBookmark, null);

                case LinkType.WindowsAbsolutePath:
                    return (Errors.LocalFilePath(relativeTo, path), null, null, null, LinkType.WindowsAbsolutePath, null);

                case LinkType.RelativePath:
                    // Resolve path relative to docset
                    var pathToDocset = ResolveToDocsetRelativePath(path, relativeTo);

                    // resolve from redirection files
                    if (relativeTo.Docset.Redirections.TryGetRedirection(pathToDocset, out var redirectFile))
                    {
                        return (null, redirectFile, query, fragment, LinkType.RelativePath, pathToDocset);
                    }

                    var file = Document.CreateFromFile(relativeTo.Docset, pathToDocset);

                    // try to resolve with .md for landing page
                    if (file is null && _forLandingPage)
                    {
                        pathToDocset = ResolveToDocsetRelativePath($"{path}.md", relativeTo);
                        file = Document.CreateFromFile(relativeTo.Docset, pathToDocset);
                    }

                    return (file != null ? null : (_forLandingPage ? null : Errors.FileNotFound(new SourceInfo<string>(path, href))), file, query, fragment, null, pathToDocset);

                default:
                    return default;
            }
        }

        private string ResolveToDocsetRelativePath(string path, Document relativeTo)
        {
            var docsetRelativePath = PathUtility.NormalizeFile(Path.Combine(Path.GetDirectoryName(relativeTo.FilePath), path));
            if (!File.Exists(Path.Combine(relativeTo.Docset.DocsetPath, docsetRelativePath)))
            {
                foreach (var (alias, aliasPath) in relativeTo.Docset.ResolveAlias)
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
    }
}
