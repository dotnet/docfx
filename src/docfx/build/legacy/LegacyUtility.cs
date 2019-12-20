// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyUtility
    {
        public static string ToLegacyOutputPathRelativeToBasePath(this Document doc, Context context, Docset docset, PublishItem manifestItem)
        {
            var outputPath = manifestItem.Path;
            if (doc.ContentType == ContentType.Resource && !doc.Docset.Config.Output.CopyResources)
            {
                outputPath = context.DocumentProvider.GetOutputPath(doc.FilePath, manifestItem.Monikers);
            }
            var legacyOutputFilePathRelativeToBasePath = Path.GetRelativePath(
                string.IsNullOrEmpty(docset.Config.BasePath.RelativePath) ? "." : docset.Config.BasePath.RelativePath, outputPath);

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
        }

        public static string ToLegacySiteUrlRelativeToBasePath(this Document doc, Docset docset)
        {
            var legacySiteUrlRelativeToBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToBasePath.StartsWith(docset.Config.BasePath.ToString(), PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToBasePath = legacySiteUrlRelativeToBasePath.Substring(1);
                legacySiteUrlRelativeToBasePath = Path.GetRelativePath(
                    string.IsNullOrEmpty(docset.Config.BasePath.RelativePath) ? "." : docset.Config.BasePath.RelativePath,
                    string.IsNullOrEmpty(legacySiteUrlRelativeToBasePath) ? "." : legacySiteUrlRelativeToBasePath);
            }

            return PathUtility.NormalizeFile(
                Path.GetFileNameWithoutExtension(doc.FilePath.Path).Equals("index", PathUtility.PathComparison)
                && doc.ContentType != ContentType.Resource
                ? $"{legacySiteUrlRelativeToBasePath}/index"
                : legacySiteUrlRelativeToBasePath);
        }

        public static string ChangeExtension(string filePath, string extension, string[] acceptableExtension = null)
        {
            acceptableExtension = acceptableExtension ?? new string[] { ".raw.page.json", ".mta.json" };
            if (!acceptableExtension.Any(ext =>
            {
                if (filePath.EndsWith(ext))
                {
                    filePath = $"{filePath.Substring(0, filePath.Length - ext.Length)}{extension}";
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
