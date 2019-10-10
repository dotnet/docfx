// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public static string ToLegacyOutputPathRelativeToSiteBasePath(this Document doc, Docset docset, PublishItem manifestItem)
        {
            var outputPath = manifestItem.Path;
            if (doc.ContentType == ContentType.Resource && !doc.Docset.Config.Output.CopyResources)
            {
                outputPath = doc.GetOutputPath(manifestItem.Monikers, docset.SiteBasePath, isPage: false);
            }
            var legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.SiteBasePath, outputPath);
            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacySiteUrlRelativeToSiteBasePath(this Document doc, Docset docset)
        {
            var legacySiteUrlRelativeToSiteBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToSiteBasePath.StartsWith($"/{docset.SiteBasePath}", PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToSiteBasePath = Path.GetRelativePath(
                    docset.SiteBasePath, legacySiteUrlRelativeToSiteBasePath.Substring(1));
            }
            if (legacySiteUrlRelativeToSiteBasePath.StartsWith("/"))
            {
                legacySiteUrlRelativeToSiteBasePath = legacySiteUrlRelativeToSiteBasePath.Substring(1);
            }
            return PathUtility.NormalizeFile(
                Path.GetFileNameWithoutExtension(doc.FilePath.Path).Equals("index", PathUtility.PathComparison)
                && doc.ContentType != ContentType.Resource
                ? $"{legacySiteUrlRelativeToSiteBasePath}/index"
                : legacySiteUrlRelativeToSiteBasePath);
        }

        public static string ChangeExtension(string filePath, string extension, string[] acceptableExtension = null)
        {
            acceptableExtension = acceptableExtension ?? new string[] { "raw.page.json", "mta.json" };
            if (!acceptableExtension.Any(ext =>
            {
                if (filePath.EndsWith(ext))
                {
                    filePath = Path.ChangeExtension(filePath.Substring(0, filePath.Length - ext.Length), extension);
                    return true;
                }
                return false;
            }))
            {
                filePath = Path.ChangeExtension(filePath, extension);
            }
            return filePath;
        }
    }
}
