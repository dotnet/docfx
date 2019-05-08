// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (Progress.Start("Restore dependencies"))
            {
                var repository = Repository.Create(docsetPath);
                Telemetry.SetRepository(repository?.Remote, repository?.Branch);

                var restoredDocsets = new ConcurrentDictionary<string, Task<DependencyLockModel>>(PathUtility.PathComparer);
                var localeToRestore = LocalizationUtility.GetLocale(repository?.Remote, repository?.Branch, options);

                await RestoreDocset(docsetPath, rootRepository: repository);

                Task<DependencyLockModel> RestoreDocset(string docset, bool root = true, Repository rootRepository = null, DependencyLockModel dependencyLock = null)
                {
                    return restoredDocsets.GetOrAdd(docset + dependencyLock?.Commit, async k =>
                    {
                        var (errors, config) = ConfigLoader.TryLoad(docset, options, localeToRestore, extend: false);

                        if (root)
                        {
                            report.Configure(config);
                        }
                        report.Write(config.ConfigFileName, errors);

                        // no need to restore child docsets' loc repository
                        return await RestoreOneDocset(
                            docset,
                            localeToRestore,
                            config,
                            (subDocset, subDependencyLock) => RestoreDocset(subDocset, root: false, dependencyLock: subDependencyLock),
                            rootRepository,
                            dependencyLock,
                            root);
                    });
                }
            }

            async Task<DependencyLockModel> RestoreOneDocset(
                string docset,
                string locale,
                Config config,
                Func<string, DependencyLockModel, Task<DependencyLockModel>> restoreChild,
                Repository rootRepository,
                DependencyLockModel dependencyLock,
                bool root)
            {
                // restore extend url firstly
                // no need to extend config
                await ParallelUtility.ForEach(
                    config.Extend.Where(HrefUtility.IsHttpHref),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config));

                // extend the config before loading
                var (errors, extendedConfig) = ConfigLoader.TryLoad(docset, options, locale, extend: true);
                report.Write(extendedConfig.ConfigFileName, errors);

                // restore and load dependency lock if need
                if (HrefUtility.IsHttpHref(extendedConfig.DependencyLock))
                    await RestoreFile.Restore(extendedConfig.DependencyLock, extendedConfig);

                if (root)
                    dependencyLock = DependencyLock.Load(docset, extendedConfig.DependencyLock);

                // restore git repos includes dependency repos, theme repo and loc repos
                var gitVersions = await RestoreGit.Restore(extendedConfig, restoreChild, locale, rootRepository, dependencyLock);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(HrefUtility.IsHttpHref).ToList();
                await RestoreFile.Restore(restoreUrls, extendedConfig);

                var generatedLock = new DependencyLockModel
                {
                    Git = gitVersions.OrderBy(g => g.Key).ToDictionary(k => k.Key, v => v.Value),
                };

                // save dependency lock if it's root entry
                if (root)
                {
                    var dependencyLockFilePath = string.IsNullOrEmpty(extendedConfig.DependencyLock) ? AppData.GetDependencyLockFile(docset, locale) : extendedConfig.DependencyLock;
                    DependencyLock.Save(docset, dependencyLockFilePath, generatedLock);
                }

                return generatedLock;
            }
        }
    }
}
