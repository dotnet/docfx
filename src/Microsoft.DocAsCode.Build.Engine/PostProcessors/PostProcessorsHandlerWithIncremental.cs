// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

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
            _innerHandler = innerPostProcessorsHandler ?? throw new ArgumentNullException(nameof(innerPostProcessorsHandler));
            _increContext = increContext ?? throw new ArgumentNullException(nameof(increContext));
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
                PostHandle(manifest, increItems);
            }
        }

        #region Handle related

        private void PreHandle(Manifest manifest, List<PostProcessor> postProcessors, string outputFolder, List<ManifestItem> increItems, List<ManifestItem> nonIncreItems)
        {
            using (new LoggerPhaseScope("PreHandle", LogLevel.Verbose))
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

        private void PostHandle(Manifest manifest, List<ManifestItem> increItems)
        {
            using (new LoggerPhaseScope("PostHandle", LogLevel.Verbose))
            {
                if (_increContext.IsIncremental)
                {
                    using (new LoggerPhaseScope("ReplayMessages", LogLevel.Verbose))
                    {
                        foreach (var file in GetFilesToReplayMessages(increItems))
                        {
                            _increContext.LastInfo.MessageInfo.Replay(file);
                        }
                    }

                    // Add back incremental items
                    manifest.Files.AddRange(increItems);
                }

                if (_increContext.ShouldTraceIncrementalInfo)
                {
                    Logger.UnregisterListener(_increContext.CurrentInfo.MessageInfo.GetListener());

                    TraceIntermediateInfo(manifest);

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

        private void TraceIntermediateInfo(Manifest manifest)
        {
            if (_increContext.ShouldTraceIncrementalInfo)
            {
                using (new LoggerPhaseScope("TraceIntermediateInfo", LogLevel.Verbose))
                {
                    foreach (var oi in from mi in manifest.Files
                                       from oi in mi.OutputFiles.Values
                                       select oi)
                    {
                        if (oi.LinkToPath != null &&
                            oi.LinkToPath.Length > _increContext.CurrentBaseDir.Length &&
                            oi.LinkToPath.StartsWith(_increContext.CurrentBaseDir, StringComparison.Ordinal) &&
                            (oi.LinkToPath[_increContext.CurrentBaseDir.Length] == '\\' ||
                            oi.LinkToPath[_increContext.CurrentBaseDir.Length] == '/'))
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

        private HashSet<string> GetFilesToReplayMessages(List<ManifestItem> increItems)
        {
            var files = new HashSet<string>(FilePathComparer.OSPlatformSensitiveStringComparer);
            var sourcePaths = (from increItem in increItems
                               select increItem.SourceRelativePath).Distinct();
            foreach (var sourceRelativePath in sourcePaths)
            {
                files.Add(sourceRelativePath);
                var pathFromWorkingFolder = ((RelativePath)sourceRelativePath).GetPathFromWorkingFolder();
                foreach (var currentVersionInfo in _increContext.CurrentBuildInfo.Versions)
                {
                    foreach (var dep in currentVersionInfo.Dependency.GetAllIncludeDependencyFrom(pathFromWorkingFolder))
                    {
                        files.Add(((RelativePath)dep).RemoveWorkingFolder());
                    }
                }
            }
            return files;
        }

        private void CopyToCurrentCache(List<ManifestItem> increItems)
        {
            using (new LoggerPhaseScope("CopyToCurrentCache", LogLevel.Verbose))
            {
                var itemsToBeCopied = from mi in increItems
                                      from oi in mi.OutputFiles.Values
                                      where oi.LinkToPath != null && oi.LinkToPath.StartsWith(_increContext.LastBaseDir, StringComparison.Ordinal)
                                      select oi;
                Parallel.ForEach(
                    itemsToBeCopied,
                    new ParallelOptions { MaxDegreeOfParallelism = _increContext.MaxParallelism },
                    item =>
                    {
                        if (!_increContext.LastInfo.PostProcessOutputs.TryGetValue(item.RelativePath, out string cachedFileName))
                        {
                            throw new BuildCacheException($"Last incremental post processor outputs should contain {item.RelativePath}.");
                        }

                        // Copy when current base dir is not last base dir
                        if (!FilePathComparerWithEnvironmentVariable.OSPlatformSensitiveRelativePathComparer.Equals(
                            _increContext.CurrentBaseDir,
                            _increContext.LastBaseDir))
                        {
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
            using (new LoggerPhaseScope("RestoreIncrementalManifestItems", LogLevel.Verbose))
            {
                var increItems = manifest.Files.Where(i => i.IsIncremental).ToList();

                if (_increContext.IsIncremental)
                {
                    var restoredIncreItems = new List<ManifestItem>();
                    OSPlatformSensitiveDictionary<List<ManifestItem>> increItemsGroup;
                    OSPlatformSensitiveDictionary<List<ManifestItem>> lastItemsGroup;
                    using (new LoggerPhaseScope("Group", LogLevel.Verbose))
                    {
                        increItemsGroup = GroupBySourceRelativePath(increItems);
                        lastItemsGroup = GroupBySourceRelativePath(_increContext.LastInfo.ManifestItems);
                    }
                    using (new LoggerPhaseScope("Restore", LogLevel.Verbose))
                    {
                        foreach (var pair in increItemsGroup)
                        {
                            if (!lastItemsGroup.TryGetValue(pair.Key, out List<ManifestItem> cachedItems))
                            {
                                throw new BuildCacheException($"Last manifest items doesn't contain the item with source relative path '{pair.Key}.'");
                            }
                            if (cachedItems.Count != pair.Value.Count)
                            {
                                throw new BuildCacheException($"The count of items with source relative path '{pair.Key}' in last manifest doesn't match: last is {cachedItems.Count}, current is {pair.Value.Count}.");
                            }

                            // Update IsIncremental flag
                            cachedItems.ForEach(c => c.IsIncremental = true);
                            restoredIncreItems.AddRange(cachedItems);
                        }
                    }
                    using (new LoggerPhaseScope("RemoveIncrementalItems", LogLevel.Verbose))
                    {
                        // Update incremental items in manifest
                        manifest.Files.RemoveAll(m => m.IsIncremental);
                    }
                    using (new LoggerPhaseScope("AddRestoredItems", LogLevel.Verbose))
                    {
                        manifest.Files.AddRange(restoredIncreItems);
                    }

                    return restoredIncreItems;
                }

                return increItems;
            }
        }

        private static OSPlatformSensitiveDictionary<List<ManifestItem>> GroupBySourceRelativePath(IEnumerable<ManifestItem> items)
        {
            var pairs = from i in items
                        group i by new { i.SourceRelativePath, i.Group }
                        into grp
                        select new KeyValuePair<string, List<ManifestItem>>(
                            $"{grp.Key.Group}:{grp.Key.SourceRelativePath}",
                            grp.ToList());
            return new OSPlatformSensitiveDictionary<List<ManifestItem>>(pairs);
        }

        #endregion
    }
}
