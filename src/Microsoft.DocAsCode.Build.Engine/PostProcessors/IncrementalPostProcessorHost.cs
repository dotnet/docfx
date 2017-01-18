// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class IncrementalPostProcessorHost : IPostProcessorHost
    {
        private readonly IncrementalPostProcessorsContext _increContext;
        private readonly string _postProcessorName;

        public IncrementalPostProcessorHost(IncrementalPostProcessorsContext increContext, string postProcessorName)
        {
            if (increContext == null)
            {
                throw new ArgumentNullException(nameof(increContext));
            }
            if (postProcessorName == null)
            {
                throw new ArgumentNullException(nameof(postProcessorName));
            }

            _increContext = increContext;
            _postProcessorName = postProcessorName;
        }

        public Stream LoadContextInfo()
        {
            if (_increContext.LastInfo == null)
            {
                return null;
            }

            var lastPostProcessorInfo = FindPostProcessorInfo(_increContext.LastInfo, _postProcessorName);
            if (lastPostProcessorInfo.ContextInfoFile == null)
            {
                return null;
            }

            return File.OpenRead(Path.Combine(_increContext.LastBaseDir, lastPostProcessorInfo.ContextInfoFile));
        }

        public Stream SaveContextInfo()
        {
            var currentPostProcessorInfo = FindPostProcessorInfo(_increContext.CurrentInfo, _postProcessorName);
            currentPostProcessorInfo.ContextInfoFile = IncrementalUtility.CreateRandomFileName(_increContext.CurrentBaseDir);

            return File.OpenWrite(Path.Combine(_increContext.CurrentBaseDir, currentPostProcessorInfo.ContextInfoFile));
        }

        private static PostProcessorInfo FindPostProcessorInfo(PostProcessInfo postProcessInfo, string postProcessorName)
        {
            var postProcessorInfo = postProcessInfo.PostProcessorInfos.SingleOrDefault(p => p.Name == postProcessorName);
            if (postProcessorInfo == null)
            {
                throw new DocfxException($"Could not find post processor info with name {postProcessorName}.");
            }

            return postProcessorInfo;
        }
    }
}
