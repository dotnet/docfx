// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationUtility
    {
        private static readonly Regex s_nameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?|\.loc)?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The loc repo remote and branch based on localization mapping<see cref="LocalizationMapping"/>
        /// </summary>
        public static (string remote, string branch) GetLocalizedRepo(LocalizationMapping mapping, bool bilingual, string remote, string branch, string locale, string defaultLocale)
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

            var newLocale = mapping == LocalizationMapping.Repository ? $".{locale}" : ".loc";
            var newBranch = bilingual ? GetLocalizationBranch(mapping, GetBilingualBranch(branch), locale) : GetLocalizationBranch(mapping, branch, locale);

            if (remote.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return (remote, newBranch);
            }

            return ($"{remote}{newLocale}", newBranch);
        }

        public static bool TryGetLocalizedDocsetPath(string docsetPath, Config config, string locale, out string localizationDocsetPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);

            localizationDocsetPath = null;
            switch (config.Localization.Mapping)
            {
                case LocalizationMapping.Repository:
                case LocalizationMapping.Branch:
                    {
                        var repo = Repository.Create(Path.GetFullPath(docsetPath));
                        if (repo == null)
                        {
                            return false;
                        }
                        var (locRemote, locBranch) = GetLocalizedRepo(
                            config.Localization.Mapping,
                            config.Localization.Bilingual,
                            repo.Remote,
                            repo.Branch,
                            locale,
                            config.Localization.DefaultLocale);
                        localizationDocsetPath = RestoreMap.GetGitRestorePath(locRemote, locBranch);
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        if (config.Localization.Bilingual)
                        {
                            throw new NotSupportedException($"{config.Localization.Mapping} is not supporting bilingual build");
                        }
                        localizationDocsetPath = Path.Combine(docsetPath, "localization", locale);
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return true;
        }

        public static bool TryGetSourceRepository(string docsetPath, out string sourceRemote, out string sourceBranch, out string locale)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            sourceRemote = null;
            sourceBranch = null;
            locale = null;

            var repo = Repository.Create(docsetPath);
            if (repo == null || string.IsNullOrEmpty(repo.Remote))
            {
                return false;
            }

            return TryGetSourceRepository(repo.Remote, repo.Branch, out sourceRemote, out sourceBranch, out locale);
        }

        /// <summary>
        /// Get the source repo's remote and branch from loc repo based on <see cref="LocalizationMapping"/>
        /// </summary>
        public static bool TryGetSourceRepository(string remote, string branch, out string sourceRemote, out string sourceBranch, out string locale)
        {
            sourceRemote = null;
            sourceBranch = null;
            locale = null;

            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(remote, out sourceRemote, out locale))
            {
                sourceBranch = branch;
                if (TryRemoveLocale(branch, out var branchWithoutLocale, out var branchLocale))
                {
                    sourceBranch = branchWithoutLocale;
                    locale = branchLocale;
                }

                if (TryGetContributionBranch(sourceBranch, out var contributionBranch))
                {
                    sourceBranch = contributionBranch;
                }
                return true;
            }

            return locale != null;
        }

        public static bool TryGetSourceDocsetPath(string docsetPath, out string sourceDocsetPath)
        {
            sourceDocsetPath = null;

            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            if (TryGetSourceRepository(docsetPath, out var sourceRemote, out var sourceBranch, out var locale))
            {
                sourceDocsetPath = RestoreMap.GetGitRestorePath(sourceRemote, sourceBranch);
                return true;
            }

            return false;
        }

        public static (Error error, string content, Document file) TryResolveContentFromHistory(GitCommitProvider gitCommitProvider, Docset docset, string pathToDocset)
        {
            // try to resolve from source repo's git history
            var fallbackDocset = GetFallbackDocset();
            if (fallbackDocset != null)
            {
                var (repo, pathToRepo, commits) = gitCommitProvider.GetCommitHistoryNoCache(Path.Combine(fallbackDocset.DocsetPath, pathToDocset), 2);
                if (repo != null)
                {
                    var repoPath = PathUtility.NormalizeFolder(repo.Path);
                    if (commits.Count > 1)
                    {
                        // the latest commit would be deleting it from repo
                        if (GitUtility.TryGetContentFromHistory(repoPath, pathToRepo, commits[1].Sha, out var content))
                        {
                            var (error, doc) = Document.TryCreate(fallbackDocset, pathToDocset, isFromHistory: true);
                            return (error, content, doc);
                        }
                    }
                }
            }

            return default;

            Docset GetFallbackDocset()
            {
                if (docset.LocalizationDocset != null)
                {
                    // source docset in loc build
                    return docset;
                }

                if (docset.FallbackDocset != null)
                {
                    // localized docset in loc build
                    return docset.FallbackDocset;
                }

                // source docset in source build
                return null;
            }
        }

        public static bool TryGetContributionBranch(string docset, out string contributionBranch, out Repository repo)
        {
            contributionBranch = null;

            repo = Repository.Create(docset);
            if (repo == null)
            {
                return false;
            }

            return TryGetContributionBranch(repo.Branch, out contributionBranch);
        }

        public static bool TryGetContributionBranch(string branch, out string contributionBranch)
        {
            contributionBranch = null;
            string locale = null;
            if (string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(branch, out var branchWithoutLocale, out locale))
            {
                branch = branchWithoutLocale;
            }

            if (branch.EndsWith("-sxs"))
            {
                contributionBranch = branch.Substring(0, branch.Length - 4);
                if (!string.IsNullOrEmpty(locale))
                {
                    contributionBranch = contributionBranch + $".{locale}";
                }
                return true;
            }

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

        public static IReadOnlyList<Document> GetTableOfContentsScope(this Docset docset, TableOfContentsMap tocMap)
        {
            Debug.Assert(tocMap != null);

            var result = docset.BuildScope.Where(d => d.ContentType == ContentType.TableOfContents).ToList();

            if (!docset.IsLocalized())
            {
                return result;
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            var fallbackTocs = new List<Document>();
            foreach (var toc in result)
            {
                if (tocMap.TryFindParents(toc, out var parents))
                {
                    fallbackTocs.AddRange(parents);
                }
            }

            result.AddRange(fallbackTocs);

            return result;
        }

        public static HashSet<Document> GetScanScope(this Docset docset)
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

        public static (List<Error> errors, Config config) GetBuildConfig(string docset, CommandLineOptions options)
        {
            if (TryGetSourceRepository(docset, out var sourceRemote, out var sourceBranch, out var locale))
            {
                var sourceDocsetPath = RestoreMap.GetGitRestorePath(sourceRemote, sourceBranch);
                return ConfigLoader.Load(sourceDocsetPath, options, locale);
            }

            return ConfigLoader.Load(docset, options);
        }

        public static (bool fromUrl, string path) GetFileRestorePath(this Docset docset, string url)
        {
            return RestoreMap.GetFileRestorePath(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static string GetBuildLocale(string docset, CommandLineOptions options)
        {
            return TryGetSourceRepository(docset, out _, out _, out var locale) ? locale : options.Locale;
        }

        public static bool IsLocalized(this Docset docset) => docset.FallbackDocset != null;

        public static bool IsLocalizedBuild(this Docset docset) => docset.FallbackDocset != null || docset.LocalizationDocset != null;

        public static (string remote, string branch) GetLocalizedTheme(string theme, string locale, string defaultLocale)
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

            if (remote.EndsWith($".{locale}", StringComparison.OrdinalIgnoreCase))
            {
                return (remote, branch);
            }

            return ($"{remote}.{locale}", branch);
        }

        public static bool TryRemoveLocale(string name, out string nameWithoutLocale, out string locale)
        {
            nameWithoutLocale = null;
            locale = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var match = s_nameWithLocale.Match(name);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                locale = match.Groups[1].Value.Substring(1).ToLowerInvariant();
                nameWithoutLocale = name.Substring(0, name.Length - match.Groups[1].Value.Length);

                return true;
            }

            return false;
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

            return $"{sourceBranch}.{locale}";
        }
    }
}
