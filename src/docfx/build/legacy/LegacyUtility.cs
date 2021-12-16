// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal static class LegacyUtility
{
    public static string ToLegacyOutputPathRelativeToBasePath(this FilePath doc, LegacyContext context, PublishItem manifestItem)
    {
        var outputPath = manifestItem.Path;
        if (outputPath is null || (context.DocumentProvider.GetContentType(doc) == ContentType.Resource && !context.Config.SelfContained))
        {
            outputPath = context.DocumentProvider.GetOutputPath(doc);
        }
        var legacyOutputFilePathRelativeToBasePath = Path.GetRelativePath(
            string.IsNullOrEmpty(context.Config.BasePath) ? "." : context.Config.BasePath, outputPath);

        return PathUtility.NormalizeFile(legacyOutputFilePathRelativeToBasePath);
    }

    public static string ToLegacySiteUrlRelativeToBasePath(this FilePath doc, LegacyContext context)
    {
        var legacySiteUrlRelativeToBasePath = context.DocumentProvider.GetSiteUrl(doc);
        if (legacySiteUrlRelativeToBasePath.StartsWith(context.Config.BasePath.ValueWithLeadingSlash, PathUtility.PathComparison))
        {
            legacySiteUrlRelativeToBasePath = legacySiteUrlRelativeToBasePath[1..];
            legacySiteUrlRelativeToBasePath = Path.GetRelativePath(
                string.IsNullOrEmpty(context.Config.BasePath) ? "." : context.Config.BasePath,
                string.IsNullOrEmpty(legacySiteUrlRelativeToBasePath) ? "." : legacySiteUrlRelativeToBasePath);
        }

        if (context.Config.UrlType == UrlType.Docs &&
            Path.GetFileNameWithoutExtension(doc.Path).Equals("index", PathUtility.PathComparison) &&
            context.DocumentProvider.GetContentType(doc) != ContentType.Resource)
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
                filePath = $"{filePath[..^ext.Length]}{extension}";
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
