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
                var restoredDocsets = new ConcurrentHashSet<string>(PathUtility.PathComparer);
                var localeToRestore = LocalizationUtility.GetBuildLocale(repository, options);

                await RestoreDocset(docsetPath, rootRepository: repository);

                async Task RestoreDocset(string docset, bool root = true, Repository rootRepository = null, DependencyLock dependencyLock = null)
                {
                    if (restoredDocsets.TryAdd(docset + dependencyLock?.Commit))
                    {
                        var (errors, config) = ConfigLoader.TryLoad(docset, options, localeToRestore, extend: false);
                        report.Write(errors);

                        if (root)
                        {
                            report.Configure(docsetPath, config);
                        }

                        // no need to restore child docsets' loc repository
                        await RestoreOneDocset(
                            docset,
                            localeToRestore,
                            config,
                            async (subDocset, subDependencyLock) => await RestoreDocset(subDocset, root: false, dependencyLock: subDependencyLock),
                            rootRepository,
                            dependencyLock);
                    }
                }
            }

            async Task RestoreOneDocset(
                string docset,
                string locale,
                Config config,
                Func<string, DependencyLock, Task> restoreChild,
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
                await RestoreGit.Restore(extendedConfig, restoreChild, locale, @implicit, rootRepository, dependencyLock);

                // restore urls except extend url
                await ParallelUtility.ForEach(
                    extendedConfig.GetFileReferences().Where(HrefUtility.IsHttpHref),
                    restoreUrl => RestoreFile.Restore(restoreUrl, extendedConfig, @implicit));
            }
        }
    }
}
