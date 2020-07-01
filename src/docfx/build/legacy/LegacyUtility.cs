// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyUtility
    {
        public static string ToLegacyOutputPathRelativeToBasePath(this Document doc, Context context, PublishItem manifestItem)
        {
            var outputPath = manifestItem.Path;
            if (outputPath is null || (doc.ContentType == ContentType.Resource && !context.Config.CopyResources))
            {
                outputPath = context.DocumentProvider.GetOutputPath(doc.FilePath);
            }
            var legacyOutputFilePathRelativeToBasePath = Path.GetRelativePath(
                string.IsNullOrEmpty(context.Config.BasePath) ? "." : context.Config.BasePath, outputPath);

            return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
        }

        public static string ToLegacySiteUrlRelativeToBasePath(this Document doc, Context context)
        {
            var legacySiteUrlRelativeToBasePath = doc.SiteUrl;
            if (legacySiteUrlRelativeToBasePath.StartsWith(context.Config.BasePath.ValueWithLeadingSlash, PathUtility.PathComparison))
            {
                legacySiteUrlRelativeToBasePath = legacySiteUrlRelativeToBasePath.Substring(1);
                legacySiteUrlRelativeToBasePath = Path.GetRelativePath(
                    string.IsNullOrEmpty(context.Config.BasePath) ? "." : context.Config.BasePath,
                    string.IsNullOrEmpty(legacySiteUrlRelativeToBasePath) ? "." : legacySiteUrlRelativeToBasePath);
            }

            if (context.Config.OutputUrlType == OutputUrlType.Docs &&
                Path.GetFileNameWithoutExtension(doc.FilePath.Path).Equals("index", PathUtility.PathComparison) && doc.ContentType != ContentType.Resource)
            {
                legacySiteUrlRelativeToBasePath = $"{legacySiteUrlRelativeToBasePath}/index";
            }

            return PathUtility.NormalizeFile(legacySiteUrlRelativeToBasePath);
        }

        public static string ChangeExtension(string filePath, string extension, string[]? acceptableExtension = null)
        {
            acceptableExtension ??= new string[] { ".raw.page.json", ".mta.json" };
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
