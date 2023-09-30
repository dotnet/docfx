// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.HtmlToPdf;

public static class ManifestUtility
{
    public static string GetRelativePath(ManifestItem item, OutputType type)
    {
        if (item?.Output == null)
        {
            return null;
        }
        switch (type)
        {
            case OutputType.Html:
                if (item.Output.TryGetValue(BuildToolConstants.OutputFileExtensions.ContentHtmlExtension, out OutputFileInfo content))
                {
                    return content.RelativePath;
                }
                break;
            case OutputType.TocJson:
                if (item.Output.TryGetValue(BuildToolConstants.OutputFileExtensions.TocFileExtension, out content))
                {
                    return content.RelativePath;
                }
                break;
            case OutputType.RawPageJson:
                if (item.Output.TryGetValue(BuildToolConstants.OutputFileExtensions.ContentRawPageExtension, out content))
                {
                    return content.RelativePath;
                }
                break;
            case OutputType.Resource:
                if (item.Output.TryGetValue(ManifestConstants.BuildManifestItem.OutputResource, out content))
                {
                    return content.RelativePath;
                }
                break;
        }

        return null;
    }

    public static string GetAssetId(ManifestItem manifestItem)
    {
        Guard.ArgumentNotNull(manifestItem, nameof(manifestItem));
        var documentType = GetDocumentType(manifestItem);
        string assetId;
        switch (documentType)
        {
            case ManifestItemType.Content:
                assetId = GetRelativePath(manifestItem, OutputType.Html) ?? GetRelativePath(manifestItem, OutputType.RawPageJson);
                break;

            case ManifestItemType.Resource:
                assetId = GetRelativePath(manifestItem, OutputType.Resource);
                break;

            case ManifestItemType.Toc:
                assetId = GetRelativePath(manifestItem, OutputType.TocJson);
                break;

            default:
                throw new NotSupportedException($"{nameof(ManifestItemType)} {documentType} is not supported.");
        }

        if (assetId == null)
        {
            throw new ArgumentException($"Invalid manifest item: {manifestItem}", nameof(manifestItem));
        }

        return assetId;
    }

    public static ManifestItemType GetDocumentType(ManifestItem item)
    {
        var type = item.Type;
        if (Enum.TryParse(type, out ManifestItemType actualType))
        {
            return actualType;
        }

        return ManifestItemType.Content;
    }
}

public enum OutputType
{
    Html,
    TocJson,
    RawPageJson,
    Resource
}

public enum ManifestItemType
{
    Toc,
    Content,
    Resource,
}
