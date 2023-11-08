// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;

namespace Docfx.Build.TableOfContents;

static class Utility
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

        if (Constants.TableOfContents.MarkdownTocFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
        {
            return TocFileType.Markdown;
        }
        if (Constants.TableOfContents.YamlTocFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))
        {
            return TocFileType.Yaml;
        }

        return TocFileType.None;
    }
}
