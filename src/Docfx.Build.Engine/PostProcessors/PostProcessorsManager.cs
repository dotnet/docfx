﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition.Hosting;
using Docfx.Common;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

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

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        var updatedMetadata = metadata;
        foreach (var postProcessor in _postProcessors)
        {
            using (new LoggerPhaseScope($"Prepare metadata in post processor {postProcessor.ContractName}", LogLevel.Verbose))
            {
                updatedMetadata = postProcessor.Processor.PrepareMetadata(updatedMetadata);
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
                Processor = new HtmlPostProcessor()
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
