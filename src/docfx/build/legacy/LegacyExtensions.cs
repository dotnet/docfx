// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyExtensions
    {
        public static string ToLegacyPathRelativeToBasePath(this Document doc, Docset docset)
        {
            return PathUtility.NormalizeFile(Path.GetRelativePath(docset.Config.SourceBasePath ?? string.Empty, doc.FilePath));
        }

        public static string ToLegacyOutputPathRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacyOutputFilePathRelativeToSiteBasePath = doc.OutputPath;
            if (legacyOutputFilePathRelativeToSiteBasePath.StartsWith(docset.Config.SiteBasePath, StringComparison.Ordinal))
            {
                legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, doc.OutputPath);
            }

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacySiteUrlRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacyOutputFilePathRelativeToSiteBasePath = doc.SiteUrl;
            if (legacyOutputFilePathRelativeToSiteBasePath.StartsWith($"/{docset.Config.SiteBasePath}", StringComparison.Ordinal))
            {
                legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, doc.SiteUrl.Substring(1));
            }

            return PathUtility.NormalizeFile(doc.FilePath.EndsWith("index.md", StringComparison.OrdinalIgnoreCase) ? $"{legacyOutputFilePathRelativeToSiteBasePath}index" : legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacyOutputPath(this LegacyManifestOutputItem legacyManifestOutputItem, Docset docset)
        {
            return Path.Combine(docset.Config.SiteBasePath ?? string.Empty, legacyManifestOutputItem.OutputPathRelativeToSiteBasePath);
        }

        public static string GetAbsoluteOutputPathFromRelativePath(this Docset docset, string relativePath)
        {
            return Path.Combine(docset.DocsetPath, docset.Config.Output.Path, relativePath);
        }
    }
}
