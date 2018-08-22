// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class Resolve
    {
        public static (Error error, string content, Document file) TryResolveContent(this Document relativeTo, string href)
        {
            var (error, file, redirect, _, _) = TryResolveFile(relativeTo, href);

            if (redirect != null)
            {
                return (Errors.IncludeRedirection(relativeTo, href), null, null);
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        public static (Error error, string href, string fragment, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);

            var (error, file, redirectTo, query, fragment) = TryResolveFile(relativeTo, href);

            // Redirection
            if (redirectTo != null && relativeTo.Docset.Config.FollowRedirect)
            {
                // TODO: append query and fragment
                return (error, redirectTo, fragment, null);
            }

            // Cannot resolve the file, leave href as is
            if (file == null)
            {
                return (error, href, fragment, null);
            }

            // Self reference, don't build the file, leave href as is
            if (file == relativeTo)
            {
                if (string.IsNullOrEmpty(fragment))
                {
                    fragment = "#";
                }
                return (error, query + fragment, fragment, null);
            }

            // Link to dependent repo, don't build the file, leave href as is
            if (file.Docset != relativeTo.Docset)
            {
                return (Errors.LinkIsDependency(relativeTo, file, href), href, fragment, null);
            }

            // Make result relative to `resultRelativeTo`
            var relativePath = PathUtility.GetRelativePathToFile(resultRelativeTo.SitePath, file.SitePath);
            var relativeUrl = HrefUtility.EscapeUrl(Document.PathToRelativeUrl(relativePath, file.ContentType));

            if (redirectTo != null)
            {
                return (error, relativeUrl + query + fragment, fragment, null);
            }

            // Pages outside build scope, don't build the file, use relative href
            if (error == null && file.ContentType == ContentType.Page && !relativeTo.Docset.BuildScope.Contains(file))
            {
                return (Errors.LinkOutOfScope(relativeTo, file, href), relativeUrl + query + fragment, fragment, null);
            }

            return (error, relativeUrl + query + fragment, fragment, file);
        }

        private static (Error error, Document file, string redirectTo, string query, string fragment) TryResolveFile(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null, null);
            }

            var (path, query, fragment) = HrefUtility.SplitHref(href);
            var pathToDocset = "";

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return (null, relativeTo, null, query, fragment);
            }

            // Leave absolute URL as is
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return default;
            }

            // Leave absolute file path as is
            if (Path.IsPathRooted(path))
            {
                return (Errors.AbsoluteFilePath(relativeTo, path), null, null, null, null);
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
                return (error, redirectFile, redirectTo, query, fragment);
            }

            var file = Document.TryCreateFromFile(relativeTo.Docset, pathToDocset);

            return (file != null ? null : Errors.FileNotFound(relativeTo, path), file, null, query, fragment);
        }
    }
}
