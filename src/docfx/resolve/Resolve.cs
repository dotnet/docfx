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
            var (error, file, _) = TryResolveFile(relativeTo, href);

            return file != null ? (error, file.ReadText(), file) : default;
        }

        public static (DocfxException error, string href, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);

            var (error, file, fragmentQuery) = TryResolveFile(relativeTo, href);

            // Cannot resolve the file, leave href as is
            if (file == null || file == relativeTo)
            {
                return (error, href, null);
            }

            var resolvedHref = file.SiteUrl + fragmentQuery;

            // Make result relative to `resultRelativeTo`
            resolvedHref = PathUtility.GetRelativePathToFile(resultRelativeTo.SiteUrl, file.SiteUrl).Replace('\\', '/');

            return (error, resolvedHref + fragmentQuery, file);
        }

        private static (DocfxException error, Document file, string fragmentQuery) TryResolveFile(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null);
            }

            var (path, fragment, query) = HrefUtility.SplitHref(href);
            var fragmentQuery = fragment + query;
            var pathToDocset = "";

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return (null, relativeTo, fragmentQuery);
            }

            // Leave absolute URL as is
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return default;
            }

            // Leave absolute file path as is
            if (Path.IsPathRooted(path))
            {
                return (Errors.LinkIsAbsolute(relativeTo, path), null, null);
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

            var file = Document.TryCreate(relativeTo.Docset, pathToDocset);

            return (file != null ? null : Errors.LinkNotFound(relativeTo, path), file, fragmentQuery);
        }
    }
}
