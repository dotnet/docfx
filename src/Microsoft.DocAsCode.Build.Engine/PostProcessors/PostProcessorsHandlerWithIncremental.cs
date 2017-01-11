// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class PostProcessorsHandlerWithIncremental : IPostProcessorsHandler
    {
        private readonly IPostProcessorsHandler _innerHandler;
        private readonly IncrementalPostProcessorsContext _increContext;

        public PostProcessorsHandlerWithIncremental(IPostProcessorsHandler innerPostProcessorsHandler, IncrementalPostProcessorsContext increContext)
        {
            if (innerPostProcessorsHandler == null)
            {
                throw new ArgumentNullException(nameof(innerPostProcessorsHandler));
            }
            if (increContext == null)
            {
                throw new ArgumentNullException(nameof(increContext));
            }
            _innerHandler = innerPostProcessorsHandler;
            _increContext = increContext;
        }

        public void Handle(List<PostProcessor> postProcessors, Manifest manifest, string outputFolder)
        {
            if (postProcessors == null)
            {
                throw new ArgumentNullException(nameof(postProcessors));
            }
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (outputFolder == null)
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            // TODO: replay warning
            var increItems = manifest.Files.Where(i => i.IsIncremental).ToList();
            var nonIncreItems = manifest.Files.Where(i => !i.IsIncremental).ToList();

            PreHandle(manifest, outputFolder, increItems, nonIncreItems);
            {
                CheckNoIncrementalItems(manifest, "Before processing");
                _innerHandler.Handle(postProcessors, manifest, outputFolder);
                CheckNoIncrementalItems(manifest, "After processing");
            }
            TraceIntermediateInfo(outputFolder, increItems, nonIncreItems);
            PostHandle(manifest, increItems);
        }

        #region Handle related

        private void PreHandle(Manifest manifest, string outputFolder, List<ManifestItem> increItems, List<ManifestItem> nonIncreItems)
        {
            using (new PerformanceScope("Pre-handle in incremental post processing"))
            {
                if (_increContext.IsIncremental)
                {
                    CopyToOutput(increItems, outputFolder);

                    // Copy none incremental items to post processors
                    manifest.Files = nonIncreItems.ToList();

                    Logger.LogVerbose($"Copied {increItems.Count} incremental items from cache, prepare to handle {nonIncreItems.Count} not incremental items.");
                }
                else
                {
                    // If cannot incremental post process, set all incremental flags to false
                    foreach (var item in manifest.Files)
                    {
                        item.IsIncremental = false;
                    }
                    nonIncreItems.AddRange(increItems);
                    increItems.Clear();
                    Logger.LogVerbose("Set all incremental flags to false, since cannot support incremental post processing.");
                }
            }
        }

        private void PostHandle(Manifest manifest, List<ManifestItem> increItems)
        {
            using (new PerformanceScope("Post-handle in incremental post processing"))
            {
                if (_increContext.IsIncremental)
                {
                    // Add back incremental items
                    manifest.Files.AddRange(increItems);
                }
            }
        }

        #endregion

        #region Trace intermediate info

        private void TraceIntermediateInfo(string outputFolder, List<ManifestItem> increItems, List<ManifestItem> nonIncreItems)
        {
            if (_increContext.ShouldTraceIncrementalInfo)
            {
                using (new PerformanceScope("Trace intermediate info in incremental post processing"))
                {
                    TraceIncremental(increItems);
                    TraceNoneIncremental(outputFolder, nonIncreItems);
                }
            }
        }

        private void TraceIncremental(List<ManifestItem> increItems)
        {
            foreach (var outputRelPath in GetOutputRelativePaths(increItems))
            {
                string lastCachedRelPath;
                if (_increContext.LastInfo == null)
                {
                    throw new BuildCacheException("Last incremental post processor info should not be null.");
                }
                if (!_increContext.LastInfo.PostProcessOutputs.TryGetValue(outputRelPath, out lastCachedRelPath))
                {
                    throw new BuildCacheException($"Last incremental post processor outputs should contain {outputRelPath}.");
                }

                IncrementalUtility.RetryIO(() =>
                {
                    var lastCachedFile = Path.Combine(_increContext.LastBaseDir, lastCachedRelPath);
                    var currentCachedFileName = IncrementalUtility.GetRandomEntry(_increContext.CurrentBaseDir);

                    // Copy last cached file to current cached file
                    File.Copy(lastCachedFile, Path.Combine(_increContext.CurrentBaseDir, currentCachedFileName));
                    _increContext.CurrentInfo.PostProcessOutputs.Add(outputRelPath, currentCachedFileName);
                });
            }
        }

        private void TraceNoneIncremental(string outputFolder, List<ManifestItem> nonIncreItems)
        {
            foreach (var outputRelPath in GetOutputRelativePaths(nonIncreItems))
            {
                IncrementalUtility.RetryIO(() =>
                {
                    var outputPath = Path.Combine(outputFolder, outputRelPath);
                    var currentCachedFileName = IncrementalUtility.GetRandomEntry(_increContext.CurrentBaseDir);

                    // Copy output to current cached file
                    File.Copy(outputPath, Path.Combine(_increContext.CurrentBaseDir, currentCachedFileName));
                    _increContext.CurrentInfo.PostProcessOutputs.Add(outputRelPath, currentCachedFileName);
                });
            }
        }

        #endregion

        #region Private methods

        private void CopyToOutput(List<ManifestItem> increItems, string outputFolder)
        {
            foreach (var outputRelPath in GetOutputRelativePaths(increItems))
            {
                string lastCachedRelPath;
                if (!_increContext.LastInfo.PostProcessOutputs.TryGetValue(outputRelPath, out lastCachedRelPath))
                {
                    throw new BuildCacheException($"Last incremental post processor outputs should contain {outputRelPath}.");
                }

                IncrementalUtility.RetryIO(() =>
                {
                    // Copy last cached file to output
                    var outputPath = Path.Combine(outputFolder, outputRelPath);
                    var lastCachedFile = Path.Combine(_increContext.LastBaseDir, lastCachedRelPath);
                    var outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    File.Copy(lastCachedFile, outputPath, true);
                });
            }
        }

        private static IEnumerable<string> GetOutputRelativePaths(List<ManifestItem> items)
        {
            return from item in items
                   from output in item.OutputFiles.Values
                select output.RelativePath;
        }

        private static void CheckNoIncrementalItems(Manifest manifest, string prependString)
        {
            if (manifest.Files.Any(i => i.IsIncremental))
            {
                throw new DocfxException($"{prependString} in inner post processor handler, manifest items should not have any incremental items.");
            }
        }

        #endregion
    }
}
