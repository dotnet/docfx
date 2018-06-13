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
            var (error, file, _, _) = TryResolveFile(relativeTo, href);

            return file != null ? (error, file.ReadText(), file) : default;
        }

        public static (DocfxException error, string href, string fragment, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo)
        {
            Debug.Assert(resultRelativeTo != null);

            var (error, file, fragment, query) = TryResolveFile(relativeTo, href);

            // Cannot resolve the file, leave href as is
            // Or self bookmark
            if (file == null || file == relativeTo)
            {
                return (error, href, fragment, file);
            }

            // Make result relative to `resultRelativeTo`
            var resolvedHref = PathUtility.GetRelativePathToFile(resultRelativeTo.SitePath, file.SitePath).Replace('\\', '/');

            return (error, resolvedHref + fragment + query, fragment, file);
        }

        private static (DocfxException error, Document file, string fragment, string query) TryResolveFile(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return (Errors.LinkIsEmpty(relativeTo), null, null, null);
            }

            var (path, fragment, query) = HrefUtility.SplitHref(href);
            var pathToDocset = "";

            // Self bookmark link
            if (string.IsNullOrEmpty(path))
            {
                return (null, relativeTo, fragment, query);
            }

            // Leave absolute URL as is
            if (path.StartsWith('/') || path.StartsWith('\\'))
            {
                return default;
            }

            // Leave absolute file path as is
            if (Path.IsPathRooted(path))
            {
                return (Errors.AbsoluteFilePath(relativeTo, path), null, null, null);
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

            return (file != null ? null : Errors.FileNotFound(relativeTo, path), file, fragment, query);
        }
    }
}
