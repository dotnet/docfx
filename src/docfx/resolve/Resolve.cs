// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class Resolve
    {
        public static (string content, Document file) TryResolveContent(this Document relativeTo, string href)
        {
            var (file, _) = TryResolveFile(relativeTo, href);

            return file != null ? (file.ReadText(), file) : default;
        }

        public static (string href, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo = null)
        {
            var (file, fragmentQuery) = TryResolveFile(relativeTo, href);

            // Cannot resolve the file, leave href as is
            if (file == null)
            {
                return (href, null);
            }

            var resolvedHref = file.SiteUrl + fragmentQuery;

            if (resultRelativeTo == null)
            {
                return (resolvedHref, file);
            }

            // Make result relative to `resultRelativeTo`
            resolvedHref = PathUtility.GetRelativePathToFile(resultRelativeTo.SiteUrl, file.SiteUrl).Replace('\\', '/');

            return (resolvedHref + fragmentQuery, file);
        }

        private static (Document file, string fragmentQuery) TryResolveFile(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return default;
            }

            var (path, fragment, query) = HrefUtility.SplitHref(href);
            var fragmentQuery = fragment + query;

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return default;
            }

            // Leave absolute URL path as is
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return default;
            }

            // Leave absolute file path as is
            if (Path.IsPathRooted(path))
            {
                // TODO: report warnings
                return default;
            }

            // Leave invalid file path as is
            if (PathUtility.FilePathHasInvalidChars(path))
            {
                return default;
            }

            // Resolve path relative to docset
            if (path.StartsWith("~\\") || path.StartsWith("~/"))
            {
                return (Document.TryCreate(relativeTo.Docset, path.Substring(2)), fragmentQuery);
            }

            // Resolve path relative to input file
            var pathToDocset = Path.Combine(Path.GetDirectoryName(relativeTo.FilePath), path);

            return (Document.TryCreate(relativeTo.Docset, pathToDocset), fragmentQuery);
        }
    }
}
