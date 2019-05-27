// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;

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

        public static string ToLegacyOutputPathRelativeToSiteBasePath(this Document doc, Docset docset, PublishItem manifestItem)
        {
            var outputPath = manifestItem.Path;
            if (doc.ContentType == ContentType.Resource && !doc.Docset.Config.Output.CopyResources)
            {
                outputPath = doc.GetOutputPath(manifestItem.Monikers, docset.SiteBasePath);
            }
            var legacyOutputFilePathRelativeToSiteBasePath = Path.GetRelativePath(docset.SiteBasePath, outputPath);

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToSiteBasePath);
        }

        public static string ToLegacySiteUrlRelativeToSiteBasePath(this Document doc, Docset docset)
        {
            var legacySiteUrlRelativeToSiteBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToSiteBasePath.StartsWith($"/{docset.SiteBasePath}", PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToSiteBasePath = Path.GetRelativePath(docset.SiteBasePath, legacySiteUrlRelativeToSiteBasePath.Substring(1));
            }

            return PathUtility.NormalizeFile(
                Path.GetFileNameWithoutExtension(doc.FilePath).Equals("index", PathUtility.PathComparison) && doc.ContentType != ContentType.Resource
                ? $"{legacySiteUrlRelativeToSiteBasePath}/index"
                : legacySiteUrlRelativeToSiteBasePath);
        }

        public static string ToLegacyOutputPath(this LegacyManifestOutputItem legacyManifestOutputItem, Docset docset, string groupId)
            => Path.Combine(docset.SiteBasePath, $"{groupId}", legacyManifestOutputItem.RelativePath);

        public static string GetAbsoluteOutputPathFromRelativePath(this Docset docset, string relativePath)
        {
            return Path.Combine(docset.DocsetPath, docset.Config.Output.Path, relativePath);
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
