// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationConvention
    {
        /// <summary>
        /// The loc repo name follows below conventions:
        /// source remote                                           -->     loc remote
        /// https:://github.com/{org}/{repo-name}                   -->     https:://github.com/{org}/{repo-name}.{locale}
        /// https:://github.com/{org}/{repo-name}.{source-locale}   -->     https:://github.com/{org}/{repo-name}.{loc-locale}
        /// // TODO: org name can be different
        /// </summary>
        public static (string remote, string branch) GetLocalizationRepo(LocalizationMapping mapping, bool bilingual, string remote, string branch, string locale, string defaultLocale)
        {
            if (mapping == LocalizationMapping.Folder)
            {
                return (remote, branch);
            }

            if (string.Equals(locale, defaultLocale))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(remote))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(branch))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(locale))
            {
                return (remote, branch);
            }

            var newLocale = mapping == LocalizationMapping.Repository ? $".{locale}" : ".localization";
            var newBranch = bilingual ? GetBilingualBranch(GetLocalizationBranch(mapping, branch, locale)) : GetLocalizationBranch(mapping, branch, locale);

            if (remote.EndsWith($".{defaultLocale}", StringComparison.OrdinalIgnoreCase))
            {
                remote = remote.Substring(0, remote.Length - $".{defaultLocale}".Length);
            }

            if (remote.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return (remote, newBranch);
            }

            return ($"{remote}{newLocale}", newBranch);
        }

        public static string GetLocalizationDocsetPath(string docsetPath, Config config, string locale)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);

            var localizationDocsetPath = docsetPath;
            switch (config.Localization.Mapping)
            {
                case LocalizationMapping.Repository:
                case LocalizationMapping.RepositoryAndFolder:
                case LocalizationMapping.Branch:
                    {
                        var repo = Repository.CreateFromFolder(Path.GetFullPath(docsetPath));
                        if (repo == null)
                        {
                            return null;
                        }
                        var (locRemote, locBranch) = GetLocalizationRepo(
                            config.Localization.Mapping,
                            config.Localization.Bilingual,
                            repo.Remote,
                            repo.Branch,
                            locale,
                            config.Localization.DefaultLocale);
                        var restorePath = RestoreMap.GetGitRestorePath(locRemote, locBranch);
                        localizationDocsetPath = config.Localization.Mapping == LocalizationMapping.RepositoryAndFolder
                            ? Path.Combine(restorePath, locale)
                            : restorePath;
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        if (config.Localization.Bilingual)
                        {
                            throw new NotSupportedException($"{config.Localization.Mapping} is not supporting bilingual build");
                        }
                        localizationDocsetPath = Path.Combine(localizationDocsetPath, "localization", locale);
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return localizationDocsetPath;
        }

        public static bool TryGetContributionBranch(string branch, out string contributionBranch)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            if (branch.EndsWith("-sxs"))
            {
                contributionBranch = branch.Substring(0, branch.Length - 4);
                return true;
            }

            contributionBranch = branch;
            return false;
        }

        public static bool TryResolveDocset(this Docset docset, string file, out Docset resolvedDocset)
        {
            // resolve from localization docset
            if (docset.LocalizationDocset != null && File.Exists(Path.Combine(docset.LocalizationDocset.DocsetPath, file)))
            {
                resolvedDocset = docset.LocalizationDocset;
                return true;
            }

            // resolve from current docset
            if (File.Exists(Path.Combine(docset.DocsetPath, file)))
            {
                resolvedDocset = docset;
                return true;
            }

            // resolve from fallback docset
            if (docset.FallbackDocset != null && File.Exists(Path.Combine(docset.FallbackDocset.DocsetPath, file)))
            {
                resolvedDocset = docset.FallbackDocset;
                return true;
            }

            resolvedDocset = null;
            return false;
        }

        public static HashSet<Document> CreateScanScope(this Docset docset)
        {
            var scanScopeFilePaths = new HashSet<string>(PathUtility.PathComparer);
            var scanScope = new HashSet<Document>();

            foreach (var buildScope in new[] { docset.LocalizationDocset?.BuildScope, docset.BuildScope, docset.FallbackDocset?.BuildScope })
            {
                if (buildScope == null)
                {
                    continue;
                }

                foreach (var document in buildScope)
                {
                    if (scanScopeFilePaths.Add(document.FilePath))
                    {
                        scanScope.Add(document);
                    }
                }
            }

            return scanScope;
        }

        public static Docset GetBuildDocset(this Docset sourceDocset)
        {
            Debug.Assert(sourceDocset != null);

            return sourceDocset.LocalizationDocset ?? sourceDocset;
        }

        public static bool IsLocalized(this Docset docset)
            => docset.FallbackDocset != null;

        public static (string remote, string branch) GetLocalizationTheme(string theme, string locale, string defaultLocale)
        {
            Debug.Assert(!string.IsNullOrEmpty(theme));
            var (remote, branch) = HrefUtility.SplitGitHref(theme);

            if (string.IsNullOrEmpty(locale))
            {
                return (remote, branch);
            }

            if (string.Equals(locale, defaultLocale))
            {
                return (remote, branch);
            }

            if (remote.EndsWith($".{defaultLocale}", StringComparison.OrdinalIgnoreCase))
            {
                remote = remote.Substring(0, remote.Length - $".{defaultLocale}".Length);
            }

            if (remote.EndsWith($".{locale}", StringComparison.OrdinalIgnoreCase))
            {
                return (remote, branch);
            }

            return ($"{remote}.{locale}", branch);
        }

        private static string GetBilingualBranch(string branch) => $"{branch}-sxs";

        private static string GetLocalizationBranch(LocalizationMapping mapping, string sourceBranch, string locale)
        {
            Debug.Assert(!string.IsNullOrEmpty(sourceBranch));

            if (mapping != LocalizationMapping.Branch)
            {
                return sourceBranch;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return sourceBranch;
            }

            return $"{locale}-{sourceBranch}";
        }
    }
}
