// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public class FileLinkInfo : IFileLinkInfo
{
    public string Href { get; init; }

    public string FromFileInDest { get; init; }

    public string FromFileInSource { get; init; }

    public string ToFileInDest { get; init; }

    public string ToFileInSource { get; init; }

    public string FileLinkInSource { get; init; }

    public string FileLinkInDest { get; init; }

    public bool IsResolved => ToFileInDest != null;

    public GroupInfo GroupInfo { get; init; }

    public FileLinkInfo()
    {
    }

    public FileLinkInfo(string fromFileInSource, string fromFileInDest, string href, IDocumentBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(fromFileInSource);
        ArgumentNullException.ThrowIfNull(fromFileInDest);
        ArgumentNullException.ThrowIfNull(href);
        ArgumentNullException.ThrowIfNull(context);

        if (UriUtility.HasFragment(href) || UriUtility.HasQueryString(href))
        {
            throw new ArgumentException("fragment and query string is not supported", nameof(href));
        }

        var path = RelativePath.TryParse(href)?.UrlDecode();
        if (path == null)
        {
            throw new ArgumentException("only relative path is supported", nameof(href));
        }

        FromFileInSource = fromFileInSource;
        FromFileInDest = fromFileInDest;
        GroupInfo = context.GroupInfo;

        if (path.IsFromWorkingFolder())
        {
            var targetInSource = path;
            ToFileInSource = targetInSource.RemoveWorkingFolder();
            ToFileInDest = RelativePath.GetPathWithoutWorkingFolderChar(context.GetFilePath(targetInSource));
            FileLinkInSource = targetInSource - (RelativePath)fromFileInSource;
            if (ToFileInDest != null)
            {
                var resolved = (RelativePath)ToFileInDest - (RelativePath)fromFileInDest;
                FileLinkInDest = resolved;
                Href = resolved.UrlEncode();
            }
            else
            {
                Href = (targetInSource.RemoveWorkingFolder() - ((RelativePath)fromFileInSource).RemoveWorkingFolder()).UrlEncode();
            }
        }
        else
        {
            FileLinkInSource = path;
            ToFileInSource = ((RelativePath)fromFileInSource + path).RemoveWorkingFolder();
            FileLinkInDest = FileLinkInSource;
            Href = href;
        }
    }
}
