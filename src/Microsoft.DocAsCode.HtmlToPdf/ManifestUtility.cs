// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.HtmlToPdf
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    public static class ManifestUtility
    {
        public static string GetRelativePath(ManifestItem item, OutputType type)
        {
            if (item?.OutputFiles == null)
            {
                return null;
            }
            switch (type)
            {
                case OutputType.Html:
                    if (item.OutputFiles.TryGetValue(BuildToolConstants.OutputFileExtensions.ContentHtmlExtension, out OutputFileInfo content))
                    {
                        return content.RelativePath;
                    }
                    break;
                case OutputType.TocJson:
                    if (item.OutputFiles.TryGetValue(BuildToolConstants.OutputFileExtensions.TocFileExtension, out content))
                    {
                        return content.RelativePath;
                    }
                    break;
                case OutputType.RawPageJson:
                    if (item.OutputFiles.TryGetValue(BuildToolConstants.OutputFileExtensions.ContentRawPageExtension, out content))
                    {
                        return content.RelativePath;
                    }
                    break;
                case OutputType.Resource:
                    if (item.OutputFiles.TryGetValue(ManifestConstants.BuildManifestItem.OutputResource, out content))
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
            var type = item.DocumentType;
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
}
