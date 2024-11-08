// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using Docfx.Common;

namespace Docfx.Build.Engine;

internal static partial class ParseTemplateHelper
{
    [GeneratedRegex(@"^\s*/(.*)/\s*$")]
    private static partial Regex IsRegexPatternRegex();

    public static string ExpandMasterPage(ResourceFileReader reader, ResourceInfo info, Regex masterRegex, Regex bodyRegex)
    {
        var template = info.Content;
        var path = info.Path;
        var masterPageResourceName = ExtractMasterPageResourceName(reader, info, masterRegex).FirstOrDefault();
        template = masterRegex.Replace(template, string.Empty);
        if (masterPageResourceName != null)
        {
            using var stream = reader.GetResourceStream(masterPageResourceName);
            if (stream != null)
            {
                using var sr = new StreamReader(stream);
                var master = sr.ReadToEnd();
                if (bodyRegex.IsMatch(master))
                {
                    return bodyRegex.Replace(master, template);
                }
                else
                {
                    Logger.LogInfo($"Master page {masterPageResourceName} does not contain {{{{!body}}}} element, content in current template {path} is ignored.");
                    return master;
                }
            }
        }

        return template;
    }

    private static IEnumerable<string> ExtractMasterPageResourceName(ResourceFileReader reader, ResourceInfo info, Regex masterRegex)
    {
        var template = info.Content;
        var path = info.Path;
        foreach (Match match in masterRegex.Matches(template))
        {
            var filePath = match.Groups["file"].Value;
            foreach (var name in GetResourceName(filePath, path, reader))
            {
                yield return name;
                Logger.LogWarning($"Multiple definitions for master page found, only the first one {match.Groups[0].Value} takes effect.");
                yield break;
            }
        }
    }

    /// <summary>
    /// file can start with "./" or using regex
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetResourceName(string file, string templateName, ResourceFileReader reader)
    {
        if (string.IsNullOrWhiteSpace(file) || file == "./")
        {
            yield break;
        }

        if (file.StartsWith("./", StringComparison.Ordinal))
        {
            file = file.Substring(2);
        }

        var regexPatternMatch = IsRegexPatternRegex().Match(file);
        if (regexPatternMatch.Groups.Count > 1)
        {
            file = regexPatternMatch.Groups[1].Value;
            var resourceKey = GetRelativeResourceKey(templateName, file);
            var regex = new Regex(resourceKey, RegexOptions.IgnoreCase);
            foreach (var name in reader.Names)
            {
                if (regex.IsMatch(name))
                {
                    yield return name;
                }
            }
        }
        else
        {
            yield return GetRelativeResourceKey(templateName, file);
        }
    }

    private static string GetRelativeResourceKey(string templateName, string relativePath)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return relativePath;
        }

        // Make sure resource keys are combined using '/'
        return StringExtension.ForwardSlashCombine(StringExtension.ToNormalizedPath(Path.GetDirectoryName(templateName)), relativePath);
    }
}
