// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class Resolve
    {
        public static (DocfxException error, string content, Document file) TryResolveContent(this Document relativeTo, string href)
        {
            var (error, file, redirect, _, _) = TryResolveFile(relativeTo, href);

            if (redirect != null)
            {
                return (Errors.IncludeRedirection(relativeTo, href), null, null);
            }

            return file != null ? (error, file.ReadText(), file) : default;
        }

        public static (DocfxException error, string href, string fragment, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);

            var (error, file, redirectTo, fragment, query) = TryResolveFile(relativeTo, href);

            // Redirection
            if (!string.IsNullOrEmpty(redirectTo))
            {
                return (error, redirectTo, fragment, file);
            }

            // Cannot resolve the file, leave href as is
            if (file == null)
            {
                return (error, href, fragment, file);
            }

            // Self reference, leave href as is
            if (file == relativeTo)
            {
                if (string.IsNullOrEmpty(fragment))
                {
                    fragment = "#";
                }
                return (error, fragment + query, fragment, file);
            }

            // Master content outside build scope, leave href as is
            if (file.IsMasterContent && relativeTo.Docset.BuildScope.Contains(file))
            {
                return (Errors.LinkOutsideBuildScope(relativeTo, file, href), href, fragment, null);
            }

            // Make result relative to `resultRelativeTo`
            var relativePath = PathUtility.GetRelativePathToFile(resultRelativeTo.SitePath, file.SitePath);
            var relativeUrl = Document.PathToRelativeUrl(relativePath, file.ContentType);

            return (error, relativeUrl + fragment + query, fragment, file);
        }

        private static (DocfxException error, Document file, string redirectTo, string fragment, string query) TryResolveFile(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null, null);
            }

            var (path, fragment, query) = HrefUtility.SplitHref(href);
            var pathToDocset = "";

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return (null, relativeTo, null, fragment, query);
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

            if (relativeTo.Docset.Config.Redirections.TryGetValue(pathToDocset, out var redirectTo))
            {
                // redirectTo always is absolute href
                return (null, null, redirectTo, null, null);
            }

            var file = Document.TryCreateFromFile(relativeTo.Docset, pathToDocset);

            return (file != null ? null : Errors.FileNotFound(relativeTo, path), file, null, fragment, query);
        }
    }
}
