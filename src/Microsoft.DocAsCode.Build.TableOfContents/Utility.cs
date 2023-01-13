// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;

    internal static class Utility
    {
        public static bool IsSupportedFile(string file)
        {
            return string.Equals("toc.yml", Path.GetFileName(file), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSupportedRelativeHref(string href)
        {
            var hrefType = GetHrefType(href);
            return IsSupportedRelativeHref(hrefType);
        }

        public static bool IsSupportedRelativeHref(HrefType hrefType)
        {
            // TocFile href type can happen when homepage is set to toc.yml explicitly
            return hrefType == HrefType.RelativeFile
                || hrefType == HrefType.YamlTocFile;
        }

        public static HrefType GetHrefType(string href)
        {
            var hrefWithoutAnchor = href != null ? UriUtility.GetPath(href) : href;
            if (!PathUtility.IsRelativePath(hrefWithoutAnchor))
            {
                return HrefType.AbsolutePath;
            }
            var fileName = Path.GetFileName(hrefWithoutAnchor);
            if (string.IsNullOrEmpty(fileName))
            {
                return HrefType.RelativeFolder;
            }

            if (IsSupportedFile(hrefWithoutAnchor))
            {
                return HrefType.YamlTocFile;
            }

            return HrefType.RelativeFile;
        }
    }
}
