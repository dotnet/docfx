// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

internal class PostProcessorsHandler : IPostProcessorsHandler
{
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
