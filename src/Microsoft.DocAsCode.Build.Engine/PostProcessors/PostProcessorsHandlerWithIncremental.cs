// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Plugins;

    internal class PostProcessorsHandlerWithIncremental : IPostProcessorsHandler
    {
        private readonly IPostProcessorsHandler _innerHandler;
        private readonly IncrementalPostProcessorsContext _incrementalPostProcessorsContext;

        public PostProcessorsHandlerWithIncremental(IPostProcessorsHandler innerPostProcessorsHandler, IncrementalPostProcessorsContext incrementalPostProcessorsContext)
        {
            if (innerPostProcessorsHandler == null)
            {
                throw new ArgumentNullException(nameof(innerPostProcessorsHandler));
            }
            if (incrementalPostProcessorsContext == null)
            {
                throw new ArgumentNullException(nameof(incrementalPostProcessorsContext));
            }
            _innerHandler = innerPostProcessorsHandler;
            _incrementalPostProcessorsContext = incrementalPostProcessorsContext;
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

            // TODO: incremental things
            _innerHandler.Handle(postProcessors, manifest, outputFolder);
        }
    }
}
