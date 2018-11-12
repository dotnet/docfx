// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                var restoredDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);
                restoredDocsets.TryAdd(docsetPath, 0);

                // Root docset must have a config
                var (configErrors, config) = Config.Load(docsetPath, options, extend: false);
                ReportErrors(report, configErrors);
                report.Configure(docsetPath, config);

                await RestoreOneDocset(report, docsetPath, options, config, RestoreDocset, restoreLocRepo: true);

                async Task RestoreDocset(string docset)
                {
                    if (restoredDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out var loadErrors, out var childConfig, false))
                    {
                        await RestoreOneDocset(report, docset, options, childConfig, RestoreDocset, restoreLocRepo: false /*no need to restore child docsets' loc repository*/);
                    }
                }
            }
        }

        public static string GetRestoreRootDir(string url, string root)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var uri = new Uri(url);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(root, repo);

            // todo: encode the dir converted from url
            return PathUtility.NormalizeFolder(dir);
        }

        private static void ReportErrors(Report report, List<Error> errors)
        {
            foreach (var error in errors)
            {
                report.Write(error);
            }
        }

        private static IEnumerable<string> GetRestoreUrls(IEnumerable<string> paths)
        {
            foreach (var url in paths)
            {
                if (!string.IsNullOrEmpty(url) && HrefUtility.IsHttpHref(url))
                {
                    yield return url;
                }
            }
        }

        private static async Task RestoreOneDocset(Report report, string docsetPath, CommandLineOptions options, Config config, Func<string, Task> restoreChild, bool restoreLocRepo = false)
        {
            // restore extend url firstly
            // no need to extend config
            await ParallelUtility.ForEach(
                GetRestoreUrls(config.Extend),
                async restoreUrl =>
                {
                    await RestoreUrl.Restore(restoreUrl, config);
                });

            // extend the config before loading
            var (errors, extendedConfig) = Config.Load(docsetPath, options);
            ReportErrors(report, errors);

            // restore git repos includes dependency repos and loc repos
            await RestoreGit.Restore(docsetPath, extendedConfig, restoreChild, restoreLocRepo ? options.Locale : null);

            // restore urls except extend url
            await ParallelUtility.ForEach(
                GetRestoreUrls(extendedConfig.GetExternalReferences()),
                async restoreUrl =>
                {
                    await RestoreUrl.Restore(restoreUrl, extendedConfig);
                });
        }
    }
}
