// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class RestoreGit
    {
        public static void Restore(
            Config config, string locale, Repository repository, DependencyLockProvider dependencyLockProvider)
        {
            ParallelUtility.ForEach(
                GetGitDependencies(config, locale, repository).Distinct(),
                git => RestoreGitRepo(config, git.remote, git.branch, git.flags, dependencyLockProvider),
                Progress.Update,
                maxDegreeOfParallelism: 8);

            EnsureLocalizationContributionBranch(config, repository);
        }

        private static void EnsureLocalizationContributionBranch(Config config, Repository repository)
        {
            // When building the live-sxs branch of a loc repo, only live-sxs branch is cloned,
            // this clone process is managed outside of build, so we need to explicitly fetch the history of live branch
            // here to generate the correct contributor list.
            if (repository != null && LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch))
            {
                GitUtility.Fetch(config, repository.Path, repository.Remote, contributionBranch);
            }
        }

        private static IEnumerable<(string remote, string branch, RestoreGitFlags flags)> GetGitDependencies(
            Config config, string locale, Repository repository)
        {
            foreach (var (_, url) in config.Dependencies)
            {
                if (url.Type == PackageType.Git)
                {
                    yield return (url.Url, url.Branch, url.RestoreFlags);
                }
            }

            if (config.Template.Type == PackageType.Git)
            {
                var localizedTemplate = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.Localization.DefaultLocale);
                if (localizedTemplate.Type == PackageType.Git)
                {
                    yield return (localizedTemplate.Url, localizedTemplate.Branch, RestoreGitFlags.DepthOne);
                }
            }

            foreach (var item in GetLocalizationGitDependencies(config, locale, repository))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Get source repository or localized repository
        /// </summary>
        private static IEnumerable<(string remote, string branch, RestoreGitFlags flags)> GetLocalizationGitDependencies(
            Config config, string locale, Repository repository)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            if (string.Equals(locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (repository is null || string.IsNullOrEmpty(repository.Remote))
            {
                yield break;
            }

            if (config.Localization.Mapping == LocalizationMapping.Folder)
            {
                yield break;
            }

            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                // fallback to master
                if (fallbackBranch != "master" &&
                    !GitUtility.RemoteBranchExists(fallbackRemote, fallbackBranch, config))
                {
                    fallbackBranch = "master";
                }
                yield return (fallbackRemote, fallbackBranch, RestoreGitFlags.None);
                yield break;
            }

            // build from English
            var (remote, branch) = LocalizationUtility.GetLocalizedRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repository.Remote,
                repository.Branch,
                locale,
                config.Localization.DefaultLocale);

            yield return (remote, branch, RestoreGitFlags.None);

            if (config.Localization.Bilingual && LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch))
            {
                // Bilingual repos also depend on non bilingual branch for commit history
                yield return (repository.Remote, contributionBranch, RestoreGitFlags.Bare);
            }
        }
    }
}
