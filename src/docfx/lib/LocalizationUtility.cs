// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationUtility
    {
        // NOTE: This line assumes each build runs in a new process
        private static readonly ConcurrentHashSet<Repository> s_fetchedLocalizationRepositories = new ConcurrentHashSet<Repository>();

        private static readonly HashSet<string> s_locales = new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Except(
                CultureInfo.GetCultures(CultureTypes.NeutralCultures)).Select(c => c.Name).Concat(
                    new[] { "zh-cn", "zh-tw", "zh-hk", "zh-sg", "zh-mo" }),
            StringComparer.OrdinalIgnoreCase);

        private static readonly Regex s_nameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?|\.loc)?$", RegexOptions.IgnoreCase);
        private static readonly Regex s_lrmAdjustment = new Regex(@"(^|\s|\>)(C#|F#|C\+\+)(\s*|[.!?;:]*)(\<|[\n\r]|$)", RegexOptions.IgnoreCase);

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
                : GetFallbackRepository(repository!.Remote, repository.Branch);
            if (fallbackRemote != null)
            {
                var docsetSourceFolder = Path.GetRelativePath(repository.Path, docsetPath);
                foreach (var branch in new[] { fallbackBranch, "main" })
                {
                    if (packageResolver.TryResolvePackage(new PackagePath(fallbackRemote, branch), PackageFetchOptions.None, out var fallbackRepoPath))
                    {
                        return Path.Combine(fallbackRepoPath, docsetSourceFolder);
                    }
                }
            }
            return null;
        }

        public static string? GetLocale(Repository? repository)
        {
            return repository is null ? null : TryRemoveLocale(repository.Remote, out _, out var remoteLocale) ? remoteLocale : null;
        }

        public static bool TryGetContributionBranch(string? branch, [NotNullWhen(true)] out string? contributionBranch)
        {
            if (branch != null && branch.EndsWith("-sxs"))
            {
                contributionBranch = branch[0..^4];
                return true;
            }

            contributionBranch = null;
            return false;
        }

        public static void EnsureLocalizationContributionBranch(PreloadConfig config, Repository? repository)
        {
            // When building the live-sxs branch of a loc repo, only live-sxs branch is cloned,
            // this clone process is managed outside of build, so we need to explicitly fetch the history of live branch
            // here to generate the correct contributor list.
            if (repository != null && TryGetContributionBranch(repository.Branch, out var contributionBranch))
            {
                using (InterProcessMutex.Create(repository.Path))
                {
                    if (s_fetchedLocalizationRepositories.Contains(repository))
                    {
                        return;
                    }

                    try
                    {
                        GitUtility.Fetch(config, repository.Path, repository.Remote, $"+{contributionBranch}:{contributionBranch}", "--update-head-ok");
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (GitUtility.IsDefaultBranch(contributionBranch))
                        {
                            try
                            {
                                var defaultBranchFallbackBranch = GitUtility.GetDefaultBranchFallbackBranch(contributionBranch);
                                GitUtility.Fetch(
                                    config,
                                    repository.Path,
                                    repository.Remote,
                                    $"+{defaultBranchFallbackBranch}:{defaultBranchFallbackBranch}",
                                    "--update-head-ok");
                            }
                            catch (InvalidOperationException e)
                            {
                                throw Errors.Config.CommittishNotFound(repository.Remote, contributionBranch).ToException(e);
                            }
                        }
                        else
                        {
                            throw Errors.Config.CommittishNotFound(repository.Remote, contributionBranch).ToException(ex);
                        }
                    }

                    s_fetchedLocalizationRepositories.TryAdd(repository);
                }
            }
        }

        internal static (string? fallbackRemote, string? fallbackBranch) GetFallbackRepository(string? remote, string? branch)
        {
            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(branch))
            {
                return default;
            }

            if (TryRemoveLocale(remote, out var fallbackRemote, out _))
            {
                var fallbackBranch = branch;
                if (TryRemoveLocale(branch, out var branchWithoutLocale, out _))
                {
                    fallbackBranch = branchWithoutLocale;
                }

                if (TryGetContributionBranch(fallbackBranch, out var contributionBranch))
                {
                    fallbackBranch = contributionBranch;
                }

                return (fallbackRemote, fallbackBranch);
            }

            return default;
        }

        private static bool TryRemoveLocale(string name, [NotNullWhen(true)] out string? nameWithoutLocale, [NotNullWhen(true)] out string? locale)
        {
            var match = s_nameWithLocale.Match(name);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                locale = match.Groups[1].Value.Substring(1).ToLowerInvariant();
                nameWithoutLocale = name.Substring(0, name.Length - match.Groups[1].Value.Length).ToLowerInvariant();
                return true;
            }

            nameWithoutLocale = null;
            locale = null;
            return false;
        }

        private static string AppendLocale(string name, string locale)
        {
            var newLocale = $".{locale}";
            if (name.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            return $"{name}{newLocale}";
        }
    }
}
