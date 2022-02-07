// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build;

internal static class LocalizationUtility
{
    private static readonly HashSet<string> s_locales = new(
        CultureInfo.GetCultures(CultureTypes.AllCultures).Except(
            CultureInfo.GetCultures(CultureTypes.NeutralCultures)).Select(c => c.Name).Concat(
            new[] { "zh-cn", "zh-tw", "zh-hk", "zh-sg", "zh-mo" }), StringComparer.OrdinalIgnoreCase);

    private static readonly Regex s_nameWithLocale = new(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?)?$", RegexOptions.IgnoreCase);
    private static readonly Regex s_lrmAdjustment = new(@"(^|\s|\>)(C#|F#|C\+\+)(\s*|[.!?;:]*)(\<|[\n\r]|$)", RegexOptions.IgnoreCase);

    public static bool IsValidLocale(string locale) => s_locales.Contains(locale);

    public static string AddLeftToRightMarker(CultureInfo culture, string text)
    {
        if (!culture.TextInfo.IsRightToLeft)
        {
            return text;
        }

        // This is used to protect against C#, F# and C++ from being split up when they are at the end of line of RTL text.
        // Find a(space or >), followed by product name, followed by zero or more(spaces or punctuation), followed by a(&lt; or newline)
        // &lrm is added after name to prevent the punctuation from moving to the other end of the line.
        // This should only be run on strings that are marked as RTL
        // & lrm may be added at places other than the end of a string, and that is ok
        return s_lrmAdjustment.Replace(text, me => $"{me.Groups[1]}{me.Groups[2]}&lrm;{me.Groups[3]}{me.Groups[4]}");
    }

    public static string? GetFallbackDocsetPath(string docsetPath, Repository? repository, PackagePath? fallbackRepository, PackageResolver packageResolver)
    {
        if (repository == null)
        {
            return null;
        }

        var (fallbackRemote, fallbackBranch) = fallbackRepository?.Type == PackageType.Git
            ? (fallbackRepository?.Url, fallbackRepository?.Branch)
            : GetFallbackRepository(repository!.Url, repository.Branch);
        if (fallbackRemote != null)
        {
            var docsetSourceFolder = Path.GetRelativePath(repository.Path, docsetPath);
            var package = new PackagePath(fallbackRemote, fallbackBranch);
            var options = PackageFetchOptions.None | PackageFetchOptions.IgnoreBranchFallbackError;
            if (packageResolver.TryResolvePackage(package, options, out var fallbackRepoPath))
            {
                return Path.Combine(fallbackRepoPath, docsetSourceFolder);
            }
            else
            {
                package = new PackagePath(fallbackRemote, "main");
                return Path.Combine(packageResolver.ResolvePackage(package, options), docsetSourceFolder);
            }
        }
        return null;
    }

    public static string? GetLocale(Repository? repository)
    {
        return repository is null ? null : TryRemoveLocale(repository.Url, out _, out var remoteLocale) ? remoteLocale : null;
    }

    public static CultureInfo CreateCultureInfo(string locale)
    {
        try
        {
            return new CultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            throw Errors.Config.LocaleInvalid(locale).ToException();
        }
    }

    internal static (string? fallbackUrl, string? fallbackBranch) GetFallbackRepository(string? url, string? branch)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(branch))
        {
            return default;
        }

        if (TryRemoveLocale(url, out var fallbackUrl, out _))
        {
            var fallbackBranch = branch;
            if (TryRemoveLocale(branch, out var branchWithoutLocale, out _))
            {
                fallbackBranch = branchWithoutLocale;
            }

            return (fallbackUrl, fallbackBranch);
        }

        return default;
    }

    private static bool TryRemoveLocale(string name, [NotNullWhen(true)] out string? nameWithoutLocale, [NotNullWhen(true)] out string? locale)
    {
        var match = s_nameWithLocale.Match(name);
        if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
        {
            locale = match.Groups[1].Value[1..].ToLowerInvariant();
            nameWithoutLocale = name[..^match.Groups[1].Value.Length].ToLowerInvariant();
            return true;
        }

        nameWithoutLocale = null;
        locale = null;
        return false;
    }
}
