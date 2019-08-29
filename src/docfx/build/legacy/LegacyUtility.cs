// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyUtility
    {
        public static string ToLegacyPathRelativeToBasePath(this Document doc, Docset docset)
        {
            return PathUtility.NormalizeFile(Path.GetRelativePath(docset.Config.DocumentId.SourceBasePath, doc.FilePath.Path));
        }

        public static string ToLegacySiteUrlRelativeToSiteBasePath(this Document doc, Docset docset)
        {
            var legacySiteUrlRelativeToSiteBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToSiteBasePath.StartsWith($"/{docset.SiteBasePath}", PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToSiteBasePath = Path.GetRelativePath(
                    docset.SiteBasePath, legacySiteUrlRelativeToSiteBasePath.Substring(1));
            }

            return PathUtility.NormalizeFile(
                Path.GetFileNameWithoutExtension(doc.FilePath.Path).Equals("index", PathUtility.PathComparison)
                && doc.ContentType != ContentType.Resource
                ? $"{legacySiteUrlRelativeToSiteBasePath}/index"
                : legacySiteUrlRelativeToSiteBasePath);
        }
    }
}
