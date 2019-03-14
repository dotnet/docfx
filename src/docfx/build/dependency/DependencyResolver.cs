// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        public DependencyResolver(GitCommitProvider gitCommitProvider, BookmarkValidator bookmarkValidator, DependencyMapBuilder dependencyMapBuilder, Lazy<XrefMap> xrefMap)
        {
            _bookmarkValidator = bookmarkValidator;
            _dependencyMapBuilder = dependencyMapBuilder;
            _gitCommitProvider = gitCommitProvider;
            _xrefMap = xrefMap;
        }

        public (Error error, string content, Document file) ResolveContent(string path, Document relativeTo, DependencyType dependencyType = DependencyType.Inclusion)
        {
            var (error, content, child) = TryResolveContent(relativeTo, path);

            _dependencyMapBuilder.AddDependencyItem(relativeTo, child, dependencyType);

            return (error, content, child);
        }

        public (Error error, string link, Document file) ResolveLink(string path, Document relativeTo, Document resultRelativeTo, Action<Document> buildChild)
        {
            var (error, link, fragment, file) = TryResolveHref(relativeTo, path, resultRelativeTo);

            if (file != null && buildChild != null)
            {
                buildChild(file);
            }

            _dependencyMapBuilder.AddDependencyItem(relativeTo, file, HrefUtility.FragmentToDependencyType(fragment));
            _bookmarkValidator.AddBookmarkReference(relativeTo, file ?? relativeTo, fragment);

            return (error, link, file);
        }

        public (Error error, string href, string display, Document file) ResolveXref(string href, Document relativeTo, Document rootFile)
        {
            var (uid, query, fragment) = HrefUtility.SplitHref(href);
            string moniker = null;
            NameValueCollection queries = null;
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query.Substring(1));
                moniker = queries?["view"];
            }
            var displayProperty = queries?["displayProperty"];

            // need to url decode uid from input content
            var (error, resolvedHref, display, referencedFile) = _xrefMap.Value.Resolve(HttpUtility.UrlDecode(uid), href, displayProperty, relativeTo, rootFile, moniker);

            if (referencedFile != null)
            {
                _dependencyMapBuilder.AddDependencyItem(rootFile, referencedFile, DependencyType.UidInclusion);
            }

            if (!string.IsNullOrEmpty(resolvedHref))
            {
                var monikerQuery = !string.IsNullOrEmpty(moniker) ? $"view={moniker}" : "";
                resolvedHref = HrefUtility.MergeHref(resolvedHref, monikerQuery, fragment.Length == 0 ? "" : fragment.Substring(1));
            }
            return (error, resolvedHref, display, referencedFile);
        }

        /// <summary>
        /// Get relative url from file to the file relative to
        /// </summary>
        public string GetRelativeUrl(Document fileRelativeTo, Document file)
        {
            var relativePath = PathUtility.GetRelativePathToFile(fileRelativeTo.SitePath, file.SitePath);
            return HrefUtility.EscapeUrl(Document.PathToRelativeUrl(
                relativePath, file.ContentType, file.Schema, file.Docset.Config.Output.Json));
        }

        private (Error error, string content, Document file) TryResolveContent(Document relativeTo, string href)
        {
            var (error, file, redirect, _, _, _, pathToDocset) = TryResolveFile(relativeTo, href);

            if (redirect != null)
            {
                return (Errors.IncludeIsRedirection(relativeTo, href), null, null);
            }

            if (file == null && !string.IsNullOrEmpty(pathToDocset))
            {
                var (errorFromHistory, content, fileFromHistory) = TryResolveContentFromHistory(_gitCommitProvider, relativeTo.Docset, pathToDocset);
                if (errorFromHistory != null)
                {
                    return (error, null, null);
                }
                if (fileFromHistory != null)
                {
                    return (null, content, fileFromHistory);
                }
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        private (Error error, string href, string fragment, Document file) TryResolveHref(Document relativeTo, string href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);

            if (href.StartsWith("xref:"))
            {
                var (uidError, uidHref, _, referencedFile) = ResolveXref(href.Substring("xref:".Length), relativeTo, resultRelativeTo);
                return (uidError, uidHref, null, referencedFile);
            }

            var decodedHref = HttpUtility.UrlDecode(href);
            var (error, file, redirectTo, query, fragment, hrefType, _) = TryResolveFile(relativeTo, decodedHref);

            // Redirection
            // follow redirections
            if (redirectTo != null && !relativeTo.Docset.Legacy)
            {
                // TODO: append query and fragment to an absolute url with query and fragments may cause problems
                return (error, redirectTo + query + fragment, null, null);
            }

            if (hrefType == HrefType.WindowsAbsolutePath)
            {
                return (error, "", fragment, null);
            }

            // Cannot resolve the file, leave href as is
            if (file == null)
            {
                return (error, href, fragment, null);
            }

            // Self reference, don't build the file, leave href as is
            if (file == relativeTo)
            {
                if (relativeTo.Docset.Legacy)
                {
                    if (hrefType == HrefType.SelfBookmark)
                    {
                        return (error, query + fragment, fragment, null);
                    }
                    var selfUrl = HrefUtility.EscapeUrl(Document.PathToRelativeUrl(
                        Path.GetFileName(file.SitePath), file.ContentType, file.Schema, file.Docset.Config.Output.Json));
                    return (error, selfUrl + query + fragment, fragment, null);
                }
                if (string.IsNullOrEmpty(fragment))
                {
                    fragment = "#";
                }
                return (error, query + fragment, fragment, null);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (relativeTo.Docset.DependencyDocsets.Values.Any(v => file.Docset == v))
            {
                return (Errors.LinkIsDependency(relativeTo, file, href), href, fragment, null);
            }

            // Make result relative to `resultRelativeTo`
            var relativeUrl = GetRelativeUrl(resultRelativeTo, file);

            if (redirectTo != null)
            {
                return (error, relativeUrl + query + fragment, fragment, null);
            }

            // Pages outside build scope, don't build the file, use relative href
            if (error == null
                && (file.ContentType == ContentType.Page || file.ContentType == ContentType.TableOfContents)
                && !file.Docset.BuildScope.Contains(file))
            {
                return (Errors.LinkOutOfScope(relativeTo, file, href, file.Docset.Config.ConfigFileName), relativeUrl + query + fragment, fragment, null);
            }

            return (error, relativeUrl + query + fragment, fragment, file);
        }

        private (Error error, Document file, string redirectTo, string query, string fragment, HrefType? hrefType, string pathToDocset) TryResolveFile(Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null, null, null, null);
            }

            var (path, query, fragment) = HrefUtility.SplitHref(href);

            switch (HrefUtility.GetHrefType(href))
            {
                case HrefType.SelfBookmark:
                    return (null, relativeTo, null, query, fragment, HrefType.SelfBookmark, null);

                case HrefType.WindowsAbsolutePath:
                    return (Errors.AbsoluteFilePath(relativeTo, path), null, null, null, null, HrefType.WindowsAbsolutePath, null);

                case HrefType.RelativePath:
                    // Resolve path relative to docset
                    var pathToDocset = ResolveToDocsetRelativePath(path, relativeTo);

                    // resolve from redirection files
                    if (relativeTo.Docset.Redirections.TryGetRedirectionUrl(pathToDocset, out var redirectTo))
                    {
                        // redirectTo always is absolute href
                        //
                        // TODO: In case of file rename, we should warn if the content is not inside build scope.
                        //       But we should not warn or do anything with absolute URLs.
                        var (error, redirectFile) = Document.TryCreate(relativeTo.Docset, pathToDocset);
                        return (error, redirectFile, redirectTo, query, fragment, HrefType.RelativePath, pathToDocset);
                    }

                    var file = Document.TryCreateFromFile(relativeTo.Docset, pathToDocset);

                    return (file != null ? null : Errors.FileNotFound(relativeTo.ToString(), path), file, null, query, fragment, null, pathToDocset);

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

        private static (Error error, string content, Document file) TryResolveContentFromHistory(GitCommitProvider gitCommitProvider, Docset docset, string pathToDocset)
        {
            // try to resolve from source repo's git history
            var fallbackDocset = GetFallbackDocset();
            if (fallbackDocset != null)
            {
                var (repo, pathToRepo, commits) = gitCommitProvider.GetCommitHistoryNoCache(fallbackDocset, pathToDocset, 2);
                if (repo != null)
                {
                    var repoPath = PathUtility.NormalizeFolder(repo.Path);
                    if (commits.Count > 1)
                    {
                        // the latest commit would be deleting it from repo
                        if (GitUtility.TryGetContentFromHistory(repoPath, pathToRepo, commits[1].Sha, out var content))
                        {
                            var (error, doc) = Document.TryCreate(fallbackDocset, pathToDocset, isFromHistory: true);
                            return (error, content, doc);
                        }
                    }
                }
            }

            return default;

            Docset GetFallbackDocset()
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
}
