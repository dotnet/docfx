// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class Utility
    {
        public static bool IsSupportedFile(string file)
        {
            var fileType = GetTocFileType(file);
            if (fileType == TocFileType.Markdown || fileType == TocFileType.Yaml)
            {
                return true;
            }

            return false;
        }

        public static bool IsSupportedRelativeHref(string href)
        {
            var hrefType = GetHrefType(href);
            return IsSupportedRelativeHref(hrefType);
        }

        public static bool IsSupportedRelativeHref(HrefType hrefType)
        {
            // TocFile href type can happen when homepage is set to toc.yml explicitly
            return hrefType == HrefType.RelativeFile
                || hrefType == HrefType.YamlTocFile
                || hrefType == HrefType.MarkdownTocFile;
        }

        public static TocItemViewModel LoadSingleToc(string file)
        {
            var fileType = GetTocFileType(file);
            try
            {
                if (fileType == TocFileType.Markdown)
                {
                    return new TocItemViewModel
                    {
                        Items = MarkdownTocReader.LoadToc(EnvironmentContext.FileAbstractLayer.ReadAllText(file), file)
                    };
                }
                else if (fileType == TocFileType.Yaml)
                {
                    try
                    {
                        return new TocItemViewModel
                        {
                            Items = YamlUtility.Deserialize<TocViewModel>(file)
                        };
                    }
                    catch (YamlDotNet.Core.YamlException)
                    {
                        var tocWithMetadata = YamlUtility.Deserialize<TocViewModelWithMetadata>(file);
                        return new TocItemViewModel
                        {
                            Items = tocWithMetadata.Items,
                            Metadata = tocWithMetadata.Metadata
                        };
                    }
                }
            }
            catch (Exception e)
            {
                var message = $"{file} is not a valid TOC File: {e.Message}";
                Logger.LogError(message);
                throw new DocumentException(message, e);
            }

            throw new NotSupportedException($"{file} is not a valid TOC file, supported toc files could be \"{Constants.MarkdownTocFileName}\" or \"{Constants.YamlTocFileName}\".");
        }

        public static HrefType GetHrefType(string href)
        {
            var hrefWithoutAnchor = href != null ? UriUtility.GetPath(href) : href;
            if (!PathUtility.IsRelativePath(hrefWithoutAnchor))
            {
                return HrefType.AbsolutePath;
            }
            var fileName = Path.GetFileName(hrefWithoutAnchor);
            if (string.IsNullOrEmpty(fileName))
            {
                return HrefType.RelativeFolder;
            }

            var tocFileType = GetTocFileType(hrefWithoutAnchor);

            if (tocFileType == TocFileType.Markdown)
            {
                return HrefType.MarkdownTocFile;
            }

            if (tocFileType == TocFileType.Yaml)
            {
                return HrefType.YamlTocFile;
            }

            return HrefType.RelativeFile;
        }

        public static TocFileType GetTocFileType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return TocFileType.None;
            }

            var fileName = Path.GetFileName(filePath);

            if (Constants.MarkdownTocFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return TocFileType.Markdown;
            }
            if (Constants.YamlTocFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return TocFileType.Yaml;
            }

            return TocFileType.None;
        }
    }
}
