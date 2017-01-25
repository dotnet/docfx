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
        private const string ExcludeType = "Resource"; // TODO: use FAL to copy the resources

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
                if (increItems.Any(i => i.DocumentType.Equals(ExcludeType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new NotSupportedException($"Currently incremental post processing logic doesn't support type {ExcludeType}.");
                }

                PreHandle(manifest, postProcessors, outputFolder, increItems, nonIncreItems);
                {
                    CheckNoIncrementalItems(manifest, "Before processing");
                    _innerHandler.Handle(postProcessors, manifest, outputFolder);
                    CheckNoIncrementalItems(manifest, "After processing");
                }
                TraceIntermediateInfo(outputFolder, increItems, nonIncreItems);
                PostHandle(manifest, increItems);
            }
        }

        #region Handle related

        private void PreHandle(Manifest manifest, List<PostProcessor> postProcessors, string outputFolder, List<ManifestItem> increItems, List<ManifestItem> nonIncreItems)
        {
            using (new PerformanceScope("Pre-handle in incremental post processing"))
            {
                if (_increContext.ShouldTraceIncrementalInfo)
                {
                    var originalFileInfos = manifest.Files.Select(SourceFileInfo.FromManifestItem).ToImmutableList();
                    foreach (var postProcessor in postProcessors)
                    {
                        var host = new IncrementalPostProcessorHost(_increContext, postProcessor.ContractName, originalFileInfos);
                        ((ISupportIncrementalPostProcessor)postProcessor.Processor).PostProcessorHost = host;
                    }
                    Logger.RegisterListener(_increContext.CurrentInfo.MessageInfo.GetListener());
                }

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

                    // Update manifest items in current post processing info
                    _increContext.CurrentInfo.ManifestItems.AddRange(manifest.Files);
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
                    EnvironmentContext.FileAbstractLayer.Copy(lastCachedFile, Path.Combine(_increContext.CurrentBaseDir, currentCachedFileName));
                    _increContext.CurrentInfo.PostProcessOutputs.Add(outputRelPath, currentCachedFileName);
                });
            }
        }

        private void TraceNoneIncremental(string outputFolder, List<ManifestItem> nonIncreItems)
        {
            foreach (var outputRelPath in GetOutputRelativePaths(nonIncreItems, ExcludeType))
            {
                IncrementalUtility.RetryIO(() =>
                {
                    var outputPath = Path.Combine(outputFolder, outputRelPath);
                    var currentCachedFileName = IncrementalUtility.GetRandomEntry(_increContext.CurrentBaseDir);

                    // Copy output to current cached file
                    EnvironmentContext.FileAbstractLayer.Copy(outputPath, Path.Combine(_increContext.CurrentBaseDir, currentCachedFileName));
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
                    EnvironmentContext.FileAbstractLayer.Copy(lastCachedFile, outputPath);
                });
            }
        }

        private static IEnumerable<string> GetOutputRelativePaths(List<ManifestItem> items, string excludeType = null)
        {
            return from item in items
                   where !item.DocumentType.Equals(excludeType, StringComparison.OrdinalIgnoreCase)
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
                manifest.Files = manifest.Files.Except(increItems).ToList();
                manifest.Files.AddRange(restoredIncreItems);
                return restoredIncreItems;
            }

            return increItems;
        }

        #endregion
    }
}
