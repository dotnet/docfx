﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    internal class IncrementalPostProcessorHost : IPostProcessorHost
    {
        private readonly IncrementalPostProcessorsContext _increContext;
        private readonly string _postProcessorName;

        public IncrementalPostProcessorHost(IncrementalPostProcessorsContext increContext, string postProcessorName, IImmutableList<SourceFileInfo> sourceFileInfos)
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
            SourceFileInfos = sourceFileInfos;
            ShouldTraceIncrementalInfo = _increContext.ShouldTraceIncrementalInfo;
            IsIncremental = _increContext.IsIncremental;
        }

        #region IPostProcessorHost

        public IImmutableList<SourceFileInfo> SourceFileInfos { get; }

        public bool ShouldTraceIncrementalInfo { get; }

        public bool IsIncremental { get; }

        public Stream LoadContextInfo()
        {
            if (_increContext.LastInfo == null)
            {
                Logger.LogVerbose("Could not load last context info since last build has no incremental information.");
                return null;
            }

            var lastPostProcessorInfo = FindPostProcessorInfo(_increContext.LastInfo, _postProcessorName);
            if (lastPostProcessorInfo.ContextInfoFile == null)
            {
                Logger.LogVerbose("Could not load last context info since last build has no context file information.");
                return null;
            }

            return EnvironmentContext.FileAbstractLayer.OpenRead(Path.Combine(_increContext.LastBaseDir, lastPostProcessorInfo.ContextInfoFile));
        }

        public Stream SaveContextInfo()
        {
            if (!_increContext.ShouldTraceIncrementalInfo)
            {
                Logger.LogVerbose("Could not save current context info since should not trace incremental information.");
                return null;
            }

            var currentPostProcessorInfo = FindPostProcessorInfo(_increContext.CurrentInfo, _postProcessorName);
            currentPostProcessorInfo.ContextInfoFile = IncrementalUtility.CreateRandomFileName(_increContext.CurrentBaseDir);

            return EnvironmentContext.FileAbstractLayer.Create(Path.Combine(_increContext.CurrentBaseDir, currentPostProcessorInfo.ContextInfoFile));
        }

        #endregion

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
