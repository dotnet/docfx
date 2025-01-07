// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.SchemaDriven.Processors;

internal static class Helper
{
    private const string ContentOriginalFileKeyName = "ContentOriginalFile";
    private static readonly HashSet<string> s_locales = new(
CultureInfo.GetCultures(CultureTypes.AllCultures).Except(
CultureInfo.GetCultures(CultureTypes.NeutralCultures)).Select(c => c.Name).Concat(
new[] { "zh-cn", "zh-tw", "zh-hk", "zh-sg", "zh-mo" }),
StringComparer.OrdinalIgnoreCase);

    public static void AddFileLinkSource(this Dictionary<string, List<LinkSourceInfo>> fileLinkSources, LinkSourceInfo source)
    {
        var file = source.Target;
        if (!fileLinkSources.TryGetValue(file, out List<LinkSourceInfo> sources))
        {
            sources = [];
            fileLinkSources[file] = sources;
        }
        sources.Add(source);
    }

    public static void SetOriginalContentFile(this IProcessContext context, string path, FileAndType file)
    {
        if (!context.PathProperties.TryGetValue(path, out var properties))
        {
            properties = context.PathProperties[path] = [];
        }

        properties[ContentOriginalFileKeyName] = file;
    }

    public static FileAndType GetOriginalContentFile(this IProcessContext context, string path)
    {
        FileAndType filePath = null;
        if (context.PathProperties.TryGetValue(path, out var properties) && properties.TryGetValue(ContentOriginalFileKeyName, out var file))
        {
            filePath = file as FileAndType;
            if (filePath == null)
            {
                Logger.LogWarning($"{ContentOriginalFileKeyName} is expecting to be with type FileAndType, however its value is {file.GetType()}");
            }
        }

        return filePath ?? context.OriginalFileAndType;
    }
    public static string RemoveHostName(string url, string hostName, bool removeLocale = false)
    {
        if (string.IsNullOrEmpty(hostName))
        {
            return url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        if (!string.Equals(uri.Host, hostName, StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var path = (uri.PathAndQuery + uri.Fragment).TrimStart('/');
        if (!removeLocale)
        {
            return $"/{path}";
        }

        var slashIndex = path.IndexOf('/');
        if (slashIndex < 0)
        {
            return $"/{path}";
        }

        var firstSegment = path.Substring(0, slashIndex);
        return IsValidLocale(firstSegment)
            ? $"{path.Substring(firstSegment.Length)}"
            : $"/{path}";
    }

    private static bool IsValidLocale(string locale) => s_locales.Contains(locale);
}
