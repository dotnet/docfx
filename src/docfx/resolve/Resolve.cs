// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal delegate (string content, Document path) ResolveContent(Document relativeTo, string href);

    internal delegate string ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

    internal static class Resolve
    {
        internal static (string content, Document file) TryResolveContent(this Document relativeTo, string href)
        {
            var buildItem = TryResolveHref(relativeTo, href);
            if (buildItem == null)
            {
                return default;
            }

            return (buildItem.ReadText(), buildItem);
        }

        internal static (string href, Document file) TryResolveHref(this Document relativeTo, string href, Document resultRelativeTo = null)
        {
            var file = TryResolveHref(relativeTo, href);
            if (file == null)
            {
                return (href, null);
            }

            var (_, fragment, query) = HrefUtility.SplitHref(href);

            var resolvedHref = Uri.EscapeUriString(file.SiteUrl) + fragment + query;
            if (href[0] == '/')
            {
                return (resolvedHref, file);
            }

            if (resultRelativeTo == null)
            {
                return (resolvedHref, file);
            }

            resolvedHref = PathUtility.GetRelativePathToFile(resultRelativeTo.SiteUrl, file.SiteUrl).Replace('\\', '/');

            return (Uri.EscapeUriString(resolvedHref) + fragment + query, file);
        }

        private static Document TryResolveHref(this Document relativeTo, string href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return default;
            }

            if (!HrefUtility.IsRelativeHref(href))
            {
                return default;
            }

            var (hrefPath, _, _) = HrefUtility.SplitHref(href);
            var docset = relativeTo.Docset;

            var path = hrefPath;
            if (hrefPath[0] == '~')
            {
                if (hrefPath.Length <= 1 || (hrefPath[1] != '/' && hrefPath[1] != '\\'))
                {
                    return default;
                }
                path = hrefPath.Substring(2);

                var relativePath = Path.GetRelativePath(Path.GetDirectoryName(relativeTo.FilePath), path);
                return relativeTo.TryResolveFile(relativePath);
            }

            return relativeTo.TryResolveFile(path);
        }
    }
}
