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
        public (string content, object file) ReadFile(string path, object relativeTo, List<Error> errors, DependencyMapBuilder dependencyMapBuilder, GitCommitProvider gitCommitProvider)
        {
            Debug.Assert(relativeTo is Document);

            var (error, content, child) = TryResolveContent((Document)relativeTo, path, gitCommitProvider);

            errors.AddIfNotNull(error);

            dependencyMapBuilder?.AddDependencyItem((Document)relativeTo, child, DependencyType.Inclusion);

            return (content, child);
        }

        public string GetLink(string path, object relativeTo, object resultRelativeTo, List<Error> errors, Action<Document> buildChild, DependencyMapBuilder dependencyMapBuilder, BookmarkValidator bookmarkValidator, XrefMap xrefMap)
        {
            Debug.Assert(relativeTo is Document);
            Debug.Assert(resultRelativeTo is Document);

            var self = (Document)relativeTo;
            var (error, link, fragment, child) = TryResolveHref(self, path, (Document)resultRelativeTo, xrefMap, dependencyMapBuilder);
            errors.AddIfNotNull(error);

            if (child != null && buildChild != null)
            {
                buildChild(child);
                dependencyMapBuilder?.AddDependencyItem(self, child, HrefUtility.FragmentToDependencyType(fragment));
            }

            bookmarkValidator?.AddBookmarkReference(self, child ?? self, fragment);

            return link;
        }

        public XrefSpec ResolveXref(string uid, XrefMap xrefMap, Document file, DependencyMapBuilder dependencyMapBuilder, string moniker = null)
        {
            if (xrefMap is null)
                return null;

            var (xrefSpec, doc) = xrefMap.Resolve(uid, moniker);
            dependencyMapBuilder.AddDependencyItem(file, doc, DependencyType.UidInclusion);
            return xrefSpec;
        }

        public (Error error, string content, Document file) TryResolveContent(Document relativeTo, string href, GitCommitProvider gitCommitProvider)
        {
            var (error, file, redirect, _, _, _, pathToDocset) = TryResolveFile(relativeTo, href);

            if (redirect != null)
            {
                return (Errors.IncludeRedirection(relativeTo, href), null, null);
            }

            if (file == null && !string.IsNullOrEmpty(pathToDocset) && gitCommitProvider != null)
            {
                var (errorFromHistory, content, fileFromHistory) = LocalizationConvention.TryResolveContentFromHistory(gitCommitProvider, relativeTo.Docset, pathToDocset);
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

        public (Error error, string href, string fragment, Document file) TryResolveHref(Document relativeTo, string href, Document resultRelativeTo, XrefMap xrefMap = null, DependencyMapBuilder dependencyMapBuilder = null)
        {
            Debug.Assert(resultRelativeTo != null);

            if (href.StartsWith("xref:"))
            {
                var (uidError, uidHref, _) = TryResolveXref(href.Substring("xref:".Length), xrefMap, dependencyMapBuilder, relativeTo);
                return (uidError, uidHref, null, null);
            }

            var (error, file, redirectTo, query, fragment, isSelfBookmark, _) = TryResolveFile(relativeTo, href);

            // Redirection
            // follow redirections
            if (redirectTo != null && !relativeTo.Docset.Legacy)
            {
                // TODO: append query and fragment to an absolute url with query and fragments may cause problems
                return (error, redirectTo + query + fragment, null, null);
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
                    if (isSelfBookmark)
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
            var relativePath = PathUtility.GetRelativePathToFile(resultRelativeTo.SitePath, file.SitePath);
            var relativeUrl = HrefUtility.EscapeUrl(Document.PathToRelativeUrl(
                relativePath, file.ContentType, file.Schema, file.Docset.Config.Output.Json));

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

        public (Error error, string href, string display) TryResolveXref(string href, XrefMap xrefMap, DependencyMapBuilder dependencyMapBuilder, Document file)
        {
            if (xrefMap is null)
                return default;

            var (uid, query, fragment) = HrefUtility.SplitHref(href);
            string moniker = null;
            NameValueCollection queries = null;
            if (!string.IsNullOrEmpty(query))
            {
                queries = HttpUtility.ParseQueryString(query.Substring(1));
                moniker = queries?["view"];
            }

            // need to url decode uid from input content
            var xrefSpec = ResolveXref(HttpUtility.UrlDecode(uid), xrefMap, file, dependencyMapBuilder, moniker);
            if (xrefSpec is null)
            {
                return (Errors.UidNotFound(file, uid, href), null, null);
            }

            // fallback order:
            // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            var name = xrefSpec.GetXrefPropertyValue("name");
            var displayPropertyValue = xrefSpec.GetXrefPropertyValue(queries?["displayProperty"]);
            string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
            var monikerQuery = !string.IsNullOrEmpty(moniker) ? $"view={moniker}" : "";
            href = HrefUtility.MergeHref(xrefSpec.Href, monikerQuery, fragment.Length == 0 ? "" : fragment.Substring(1));
            return (null, href, display);
        }

        private static (Error error, Document file, string redirectTo, string query, string fragment, bool isSelfBookmark, string pathToDocset) TryResolveFile(Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null, null, false, null);
            }

            var (path, query, fragment) = HrefUtility.SplitHref(href);
            var pathToDocset = "";

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return (null, relativeTo, null, query, fragment, true, pathToDocset);
            }

            // Leave absolute URL as is
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return default;
            }

            // Leave absolute file path as is
            if (Path.IsPathRooted(path))
            {
                return (Errors.AbsoluteFilePath(relativeTo, path), null, null, null, null, false, pathToDocset);
            }

            // Leave absolute URL path as is
            if (Uri.TryCreate(path, UriKind.Absolute, out _))
            {
                return default;
            }

            // Resolve path relative to docset
            if (path.StartsWith("~\\") || path.StartsWith("~/"))
            {
                pathToDocset = path.Substring(2);
            }
            else
            {
                // Resolve path relative to input file
                pathToDocset = Path.Combine(Path.GetDirectoryName(relativeTo.FilePath), path);
            }

            // resolve from redirection files
            pathToDocset = PathUtility.NormalizeFile(pathToDocset);

            if (relativeTo.Docset.Redirections.TryGetRedirectionUrl(pathToDocset, out var redirectTo))
            {
                // redirectTo always is absolute href
                //
                // TODO: In case of file rename, we should warn if the content is not inside build scope.
                //       But we should not warn or do anything with absolute URLs.
                var (error, redirectFile) = Document.TryCreate(relativeTo.Docset, pathToDocset);
                return (error, redirectFile, redirectTo, query, fragment, false, pathToDocset);
            }

            var file = Document.TryCreateFromFile(relativeTo.Docset, pathToDocset);

            return (file != null ? null : Errors.FileNotFound(relativeTo.ToString(), path), file, null, query, fragment, false, pathToDocset);
        }
    }
}
