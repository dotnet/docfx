// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition.Hosting;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class PostProcessorsManager : IDisposable
    {
        private readonly List<PostProcessor> _postProcessors;
        private IPostProcessorsHandler _postProcessorsHandler;

        public PostProcessorsManager(CompositionHost container, ImmutableArray<string> postProcessorNames)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            if (postProcessorNames == null)
            {
                throw new ArgumentNullException(nameof(postProcessorNames));
            }
            _postProcessors = GetPostProcessor(container, postProcessorNames);
            _postProcessorsHandler = new PostProcessorsHandler();
        }

        public void IncrementalInitialize(string intermediateFolder, BuildInfo currentBuildInfo, BuildInfo lastBuildInfo, bool forcePostProcess, int maxParallelism)
        {
            if (intermediateFolder != null)
            {
                var increPostProcessorsContext = new IncrementalPostProcessorsContext(intermediateFolder, currentBuildInfo, lastBuildInfo, _postProcessors, !forcePostProcess, maxParallelism);
                _postProcessorsHandler = new PostProcessorsHandlerWithIncremental(_postProcessorsHandler, increPostProcessorsContext);
            }
        }

        public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            var updatedMetadata = metadata;
            foreach (var postProcessor in _postProcessors)
            {
                using (new LoggerPhaseScope($"Prepare metadata in post processor {postProcessor.ContractName}", LogLevel.Verbose))
                {
                    updatedMetadata = postProcessor.Processor.PrepareMetadata(metadata);
                    if (updatedMetadata == null)
                    {
                        throw new DocfxException($"Post processor {postProcessor.ContractName} should not return null metadata");
                    }
                }
            }
            return updatedMetadata;
        }

        public void Process(Manifest manifest, string outputFolder)
        {
            _postProcessorsHandler.Handle(_postProcessors, manifest, outputFolder);
        }

        private static List<PostProcessor> GetPostProcessor(CompositionHost container, ImmutableArray<string> processors)
        {
            var processorList = new List<PostProcessor>();
            AddBuildInPostProcessor(processorList);
            foreach (var processor in processors)
            {
                var p = CompositionContainer.GetExport<IPostProcessor>(container, processor);
                if (p != null)
                {
                    processorList.Add(new PostProcessor
                    {
                        ContractName = processor,
                        Processor = p
                    });
                    Logger.LogInfo($"Post processor {processor} loaded.");
                }
                else
                {
                    Logger.LogWarning($"Can't find the post processor: {processor}");
                }
            }
            return processorList;
        }

        private static void AddBuildInPostProcessor(List<PostProcessor> processorList)
        {
            processorList.Add(
                new PostProcessor
                {
                    ContractName = "html",
                    Processor = new HtmlPostProcessor
                    {
                        Handlers =
                        {
                            new ValidateBookmark(),
                            new RemoveDebugInfo(),
                        },
                    }
                });
        }

        public void Dispose()
        {
            foreach (var processor in _postProcessors)
            {
                Logger.LogVerbose($"Disposing processor {processor.ContractName} ...");
                (processor.Processor as IDisposable)?.Dispose();
            }
        }
    }
}
