// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class PostProcessorsHandler : IPostProcessorsHandler
    {
        public void Handle(List<PostProcessor> postProcessors, Manifest manifest, string outputFolder)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }
            if (outputFolder == null)
            {
                throw new ArgumentNullException(nameof(outputFolder));
            }

            foreach (var postProcessor in postProcessors)
            {
                using (new LoggerPhaseScope($"Process in post processor {postProcessor.ContractName}", false))
                using (new PerformanceScope($"Process in post processor {postProcessor.ContractName}"))
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
