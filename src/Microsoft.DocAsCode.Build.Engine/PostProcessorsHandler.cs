// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition.Hosting;

    using Microsoft.DocAsCode.Build.Common;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class PostProcessorsHandler : IDisposable
    {
        private readonly List<PostProcessor> _postProcessors;

        public PostProcessorsHandler(CompositionHost container, ImmutableArray<string> postProcessorNames)
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
        }

        public void PrepareMetadata(ImmutableDictionary<string, object> metadata)
        {
            foreach (var postProcessor in _postProcessors)
            {
                using (new LoggerPhaseScope($"Prepare metadata in post processor {postProcessor.ContractName}", false))
                using (new PerformanceScope($"Prepare metadata in post processor {postProcessor.ContractName}"))
                {
                    metadata = postProcessor.Processor.PrepareMetadata(metadata);
                    if (metadata == null)
                    {
                        throw new DocfxException($"Plugin {postProcessor.ContractName} should not return null metadata");
                    }
                }
            }
        }

        public void Handle(Manifest manifest, string outputFolder)
        {
            foreach (var postProcessor in _postProcessors)
            {
                using (new LoggerPhaseScope($"Process in post processor {postProcessor.ContractName}", false))
                using (new PerformanceScope($"Process in post processor {postProcessor.ContractName}"))
                {
                    manifest = postProcessor.Processor.Process(manifest, outputFolder);
                    if (manifest == null)
                    {
                        throw new DocfxException($"Plugin {postProcessor.ContractName} should not return null manifest");
                    }

                    // To make sure post processor won't generate duplicate output files
                    ManifestUtility.RemoveDuplicateOutputFiles(manifest.Files);
                }
            }
        }

        private static List<PostProcessor> GetPostProcessor(CompositionHost container, ImmutableArray<string> processors)
        {
            var processorList = new List<PostProcessor>();
            AddBuildInPostProcessor(processorList);
            foreach (var processor in processors)
            {
                var p = CompositionUtility.GetExport<IPostProcessor>(container, processor);
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

        private sealed class PostProcessor
        {
            public string ContractName { get; set; }
            public IPostProcessor Processor { get; set; }
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
