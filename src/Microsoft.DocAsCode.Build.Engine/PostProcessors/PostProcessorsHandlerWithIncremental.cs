// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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

            using (new LoggerPhaseScope("HandlePostProcessorsWithIncremental", LogLevel.Verbose))
            {
                var increItems = RestoreIncrementalManifestItems(manifest);
                var nonIncreItems = manifest.Files.Where(i => !i.IsIncremental).ToList();
                PreHandle(manifest, postProcessors, outputFolder, increItems, nonIncreItems);
                {
                    CheckNoIncrementalItems(manifest, "Before processing");
                    _innerHandler.Handle(postProcessors, manifest, outputFolder);
                    CheckNoIncrementalItems(manifest, "After processing");
                }
                PostHandle(manifest, increItems, outputFolder);
            }
        }

        #region Handle related

        private void PreHandle(Manifest manifest, List<PostProcessor> postProcessors, string outputFolder, List<ManifestItem> increItems, List<ManifestItem> nonIncreItems)
        {
            using (new PerformanceScope("Pre-handle in incremental post processing"))
            {
                if (_increContext.ShouldTraceIncrementalInfo)
                {
                    EnvironmentContext.FileAbstractLayerImpl =
                        FileAbstractLayerBuilder.Default
                        .ReadFromManifest(manifest, outputFolder)
                        .WriteToManifest(manifest, outputFolder, _increContext.CurrentBaseDir)
                        .Create();

                    Logger.RegisterListener(_increContext.CurrentInfo.MessageInfo.GetListener());
                }

                if (_increContext.IsIncremental)
                {
                    CopyToCurrentCache(increItems);

                    // Copy none incremental items to post processors
                    manifest.Files.Clear();
                    manifest.Files.AddRange(nonIncreItems);

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

                if (_increContext.ShouldTraceIncrementalInfo)
                {
                    var originalFileInfos = nonIncreItems.Concat(increItems).Select(SourceFileInfo.FromManifestItem).ToImmutableList();
                    foreach (var postProcessor in postProcessors)
                    {
                        var host = new IncrementalPostProcessorHost(_increContext, postProcessor.ContractName, originalFileInfos);
                        ((ISupportIncrementalPostProcessor)postProcessor.Processor).PostProcessorHost = host;
                    }
                }
            }
        }

        private void PostHandle(Manifest manifest, List<ManifestItem> increItems, string outputFolder)
        {
            using (new PerformanceScope("Post-handle in incremental post processing"))
            {
                if (_increContext.IsIncremental)
                {
                    foreach (var increItem in increItems)
                    {
                        _increContext.LastInfo.MessageInfo.Replay(increItem.SourceRelativePath);
                    }

                    // Add back incremental items
                    manifest.Files.AddRange(increItems);
                }

                if (_increContext.ShouldTraceIncrementalInfo)
                {
                    Logger.UnregisterListener(_increContext.CurrentInfo.MessageInfo.GetListener());

                    TraceIntermediateInfo(outputFolder, manifest);

                    manifest.Shrink(_increContext.CurrentBaseDir);

                    // Update manifest items in current post processing info
                    _increContext.CurrentInfo.ManifestItems.AddRange(manifest.Files);
                    _increContext.CurrentInfo.SaveManifest(_increContext.CurrentBaseDir);
                }

                if (manifest.IncrementalInfo == null)
                {
                    manifest.IncrementalInfo = new List<IncrementalInfo>();
                }
                manifest.IncrementalInfo.Add(_increContext.IncrementalInfo);
            }
        }

        #endregion

        #region Trace intermediate info

        private void TraceIntermediateInfo(string outputFolder, Manifest manifest)
        {
            if (_increContext.ShouldTraceIncrementalInfo)
            {
                using (new PerformanceScope("Trace intermediate info in incremental post processing"))
                {
                    foreach (var oi in from mi in manifest.Files
                                       from oi in mi.OutputFiles.Values
                                       select oi)
                    {
                        if (oi.LinkToPath != null &&
                            oi.LinkToPath.StartsWith(_increContext.CurrentBaseDir))
                        {
                            var cachedFileName = oi.LinkToPath.Substring(_increContext.CurrentBaseDir.Length).TrimStart('\\', '/');
                            _increContext.CurrentInfo.PostProcessOutputs[oi.RelativePath] = cachedFileName;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private methods

        private void CopyToCurrentCache(List<ManifestItem> increItems)
        {
            foreach (var item in from mi in increItems
                                 from oi in mi.OutputFiles.Values
                                 where oi.LinkToPath != null && oi.LinkToPath.StartsWith(_increContext.LastBaseDir)
                                 select oi)
            {
                string cachedFileName;
                if (!_increContext.LastInfo.PostProcessOutputs.TryGetValue(item.RelativePath, out cachedFileName))
                {
                    throw new BuildCacheException($"Last incremental post processor outputs should contain {item.RelativePath}.");
                }

                IncrementalUtility.RetryIO(() =>
                {
                    // Copy last cached file to current cache.
                    var newFileName = IncrementalUtility.GetRandomEntry(_increContext.CurrentBaseDir);
                    var currentCachedFile = Path.Combine(Environment.ExpandEnvironmentVariables(_increContext.CurrentBaseDir), newFileName);
                    var lastCachedFile = Path.Combine(Environment.ExpandEnvironmentVariables(_increContext.LastBaseDir), cachedFileName);
                    File.Copy(lastCachedFile, currentCachedFile);
                    item.LinkToPath = Path.Combine(_increContext.CurrentBaseDir, newFileName);
                });
            }
        }

        private static void CheckNoIncrementalItems(Manifest manifest, string prependString)
        {
            if (manifest.Files.Any(i => i.IsIncremental))
            {
                throw new DocfxException($"{prependString} in inner post processor handler, manifest items should not have any incremental items.");
            }
        }

        private List<ManifestItem> RestoreIncrementalManifestItems(Manifest manifest)
        {
            var increItems = manifest.Files.Where(i => i.IsIncremental).ToList();

            if (_increContext.IsIncremental)
            {
                var restoredIncreItems = new List<ManifestItem>();
                foreach (var increItem in increItems)
                {
                    var cachedItem = _increContext.LastInfo.ManifestItems.FirstOrDefault(i => i.SourceRelativePath == increItem.SourceRelativePath);
                    if (cachedItem == null)
                    {
                        throw new BuildCacheException($"Last manifest items doesn't contain the item with source relative path '{increItem.SourceRelativePath}'.");
                    }

                    // Update IsIncremental flag
                    cachedItem.IsIncremental = true;

                    restoredIncreItems.Add(cachedItem);
                }

                // Update incremental items in manifest
                manifest.Files.RemoveAll(m => increItems.Contains(m));
                manifest.Files.AddRange(restoredIncreItems);
                return restoredIncreItems;
            }

            return increItems;
        }

        #endregion
    }
}
