// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyUtility
    {
        public static void MoveFileSafe(string sourceFileName, string destFileName)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceFileName));
            Debug.Assert(!string.IsNullOrEmpty(destFileName));
            Debug.Assert(File.Exists(sourceFileName));

            Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
            File.Delete(destFileName);
            File.Move(sourceFileName, destFileName);
        }

        public static string ToLegacyPathRelativeToBasePath(this Document doc, Docset docset)
        {
            return PathUtility.NormalizeFile(Path.GetRelativePath(docset.Config.SourceBasePath ?? string.Empty, doc.FilePath));
        }

        public static string ToLegacyOutputPathRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacyOutputFilePathRelativeToSiteBasePath = doc.OutputPath;
            if (legacyOutputFilePathRelativeToSiteBasePath.StartsWith(docset.Config.SiteBasePath, PathUtility.PathComparison))
            {
                legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, doc.OutputPath);
            }

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacySiteUrlRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacyOutputFilePathRelativeToSiteBasePath = doc.SiteUrl;
            if (legacyOutputFilePathRelativeToSiteBasePath.StartsWith($"/{docset.Config.SiteBasePath}", PathUtility.PathComparison))
            {
                legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.SiteBasePath, doc.SiteUrl.Substring(1));
            }

            return PathUtility.NormalizeFile(doc.FilePath.EndsWith("index.md", PathUtility.PathComparison) ? $"{legacyOutputFilePathRelativeToSiteBasePath}/index" : legacyOutputFilePathRelativeToSiteBasePath);
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
