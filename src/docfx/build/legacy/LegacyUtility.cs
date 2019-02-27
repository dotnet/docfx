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
            if (PathUtility.NormalizeFile(sourceFileName) != PathUtility.NormalizeFile(destFileName))
            {
                PathUtility.CreateDirectoryFromFilePath(destFileName);

                File.Delete(destFileName);
                File.Move(sourceFileName, destFileName);
            }
        }

        public static string ToLegacyPathRelativeToBasePath(this Document doc, Docset docset)
        {
            return PathUtility.NormalizeFile(Path.GetRelativePath(docset.Config.DocumentId.SourceBasePath, doc.FilePath));
        }

        public static string ToLegacyOutputPathRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacyOutputFilePathRelativeToSiteBasePath = doc.SitePath;
            if (legacyOutputFilePathRelativeToSiteBasePath.StartsWith(docset.Config.DocumentId.SiteBasePath, PathUtility.PathComparison))
            {
                legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.DocumentId.SiteBasePath, legacyOutputFilePathRelativeToSiteBasePath);
            }

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacySiteUrlRelativeToBaseSitePath(this Document doc, Docset docset)
        {
            var legacySiteUrlRelativeToSiteBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToSiteBasePath.StartsWith($"/{docset.Config.DocumentId.SiteBasePath}", PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToSiteBasePath = Path.GetRelativePath(docset.Config.DocumentId.SiteBasePath, legacySiteUrlRelativeToSiteBasePath.Substring(1));
            }

            return PathUtility.NormalizeFile(
                Path.GetFileNameWithoutExtension(doc.FilePath).Equals("index", PathUtility.PathComparison)
                ? $"{legacySiteUrlRelativeToSiteBasePath}/index"
                : legacySiteUrlRelativeToSiteBasePath);
        }

        public static string ToLegacyOutputPath(this LegacyManifestOutputItem legacyManifestOutputItem, Docset docset, string groupId)
            => Path.Combine(docset.Config.DocumentId.SiteBasePath, $"{groupId}", legacyManifestOutputItem.RelativePath);

        public static string GetAbsoluteOutputPathFromRelativePath(this Docset docset, string relativePath)
        {
            return Path.Combine(docset.DocsetPath, docset.Config.Output.Path, relativePath);
        }
    }
}
