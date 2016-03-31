// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Utility;
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
        
        public static TocViewModel LoadSingleToc(string file)
        {
            var fileType = GetTocFileType(file);
            try
            {
                if (fileType == TocFileType.Markdown)
                {
                    return MarkdownTocReader.LoadToc(File.ReadAllText(file), file);
                }
                else if (fileType == TocFileType.Yaml)
                {
                    return YamlUtility.Deserialize<TocViewModel>(file);
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
            if (!PathUtility.IsRelativePath(href))
            {
                return HrefType.AbsolutePath;
            }
            var fileName = Path.GetFileName(href);
            if (string.IsNullOrEmpty(fileName))
            {
                return HrefType.RelativeFolder;
            }

            var tocFileType = GetTocFileType(href);

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
