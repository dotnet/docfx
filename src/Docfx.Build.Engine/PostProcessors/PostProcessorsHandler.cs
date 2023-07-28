// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

internal class PostProcessorsHandler : IPostProcessorsHandler
{
    public void Handle(List<PostProcessor> postProcessors, Manifest manifest, string outputFolder)
    {
        ArgumentNullException.ThrowIfNull(postProcessors);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(outputFolder);

        using (new LoggerPhaseScope("HandlePostProcessors", LogLevel.Verbose))
        {
            foreach (var postProcessor in postProcessors)
            {
                using (new LoggerPhaseScope($"Processing {postProcessor.ContractName}", LogLevel.Verbose))
                {
                    manifest = postProcessor.Processor.Process(manifest, outputFolder);
                    if (manifest == null)
                    {
                        throw new DocfxException($"Post processor {postProcessor.ContractName} should not return null manifest");
                    }

                    // To make sure post processor won't generate duplicate output files
                    ManifestUtility.RemoveDuplicateOutputFiles(manifest.Files);
                }
            }
        }
    }
}
