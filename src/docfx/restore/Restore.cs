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
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report, bool @implicit = false)
        {
            if (@implicit && options.NoRestore)
            {
                return;
            }

            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (Progress.Start("Restore dependencies"))
            {
                var repository = Repository.Create(docsetPath, branch: null);
                var restoredDocsets = new ConcurrentDictionary<string, Task<DependencyLock>>(PathUtility.PathComparer);
                var localeToRestore = LocalizationUtility.GetBuildLocale(repository, options);

                await RestoreDocset(docsetPath, rootRepository: repository);

                Task<DependencyLock> RestoreDocset(string docset, bool root = true, Repository rootRepository = null, DependencyLock dependencyLock = null)
                {
                    return restoredDocsets.GetOrAdd(docset + dependencyLock?.Commit, async k =>
                    {
                        var (errors, config) = ConfigLoader.TryLoad(docset, options, localeToRestore, extend: false);
                        report.Write(errors);

                        if (root)
                        {
                            report.Configure(docsetPath, config);
                        }

                        // no need to restore child docsets' loc repository
                        return await RestoreOneDocset(
                            docset,
                            localeToRestore,
                            config,
                            (subDocset, subDependencyLock) => RestoreDocset(subDocset, root: false, dependencyLock: subDependencyLock),
                            rootRepository,
                            dependencyLock);
                    });
                }
            }

            async Task<DependencyLock> RestoreOneDocset(
                string docset,
                string locale,
                Config config,
                Func<string, DependencyLock, Task<DependencyLock>> restoreChild,
                Repository rootRepository,
                DependencyLock dependencyLock)
            {
                // restore extend url firstly
                // no need to extend config
                await ParallelUtility.ForEach(
                    config.Extend.Where(HrefUtility.IsHttpHref),
                    restoreUrl => RestoreFile.Restore(restoreUrl, config, @implicit));

                // extend the config before loading
                var (errors, extendedConfig) = ConfigLoader.TryLoad(docset, options, locale, extend: true);
                report.Write(errors);

                // restore and load dependency lock if need
                if (HrefUtility.IsHttpHref(extendedConfig.DependencyLock))
                    await RestoreFile.Restore(extendedConfig.DependencyLock, extendedConfig, @implicit);
                dependencyLock = dependencyLock ?? await DependencyLock.Load(docset, extendedConfig.DependencyLock);

                // restore git repos includes dependency repos and loc repos
                var gitVersions = await RestoreGit.Restore(extendedConfig, restoreChild, locale, @implicit, rootRepository, dependencyLock);

                // restore urls except extend url
                var restoreUrls = extendedConfig.GetFileReferences().Where(HrefUtility.IsHttpHref).ToList();
                var downloadVersions = await RestoreFile.Restore(restoreUrls, extendedConfig, @implicit);

                return new DependencyLock(gitVersions, downloadVersions);
            }
        }
    }
}
