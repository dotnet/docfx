// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class IncrementalPostProcessorsContext
    {
        private readonly List<PostProcessor> _postProcessors;

        #region Properties

        public PostProcessInfo CurrentInfo { get; }

        public PostProcessInfo LastInfo { get; }

        public bool EnableIncremental { get; }

        public string CurrentBaseDir { get; }

        public string LastBaseDir { get; }

        /// <summary>
        /// Whether to trace the incremental info in intermediate folder
        /// </summary>
        public bool ShouldTraceIncrementalInfo { get; }

        /// <summary>
        /// Whether to post process incrementally
        /// </summary>
        public bool CanIncrementalPostProcess { get; }

        #endregion

        #region Constructor

        public IncrementalPostProcessorsContext(
            string intermediateFolder,
            BuildInfo currentBuildInfo,
            BuildInfo lastBuildInfo,
            List<PostProcessor> postProcessors,
            bool enableIncremental)
        {
            if (intermediateFolder == null)
            {
                throw new ArgumentNullException(nameof(intermediateFolder));
            }
            if (currentBuildInfo == null)
            {
                throw new ArgumentNullException(nameof(currentBuildInfo));
            }
            if (postProcessors == null)
            {
                throw new ArgumentNullException(nameof(postProcessors));
            }

            ShouldTraceIncrementalInfo = GetShouldTraceIncrementalInfo();
            CanIncrementalPostProcess = GetCanIncrementalPostProcess();
            if (ShouldTraceIncrementalInfo)
            {
                currentBuildInfo.PostProcessInfo = GeneratePostProcessInfo();
            }
            CurrentInfo = currentBuildInfo.PostProcessInfo;
            LastInfo = lastBuildInfo?.PostProcessInfo;
            CurrentBaseDir = Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName);
            LastBaseDir = lastBuildInfo == null ? null : Path.Combine(intermediateFolder, lastBuildInfo.DirectoryName);
            EnableIncremental = enableIncremental;
            _postProcessors = postProcessors;
        }

        #endregion

        #region Private methods

        private bool GetShouldTraceIncrementalInfo()
        {
            if (_postProcessors.All(p => p.Processor is ISupportIncrementalPostProcessor))
            {
                return true;
            }

            var message = $"Cannot support incremental post processors, because following post processors don't implement {nameof(ISupportIncrementalPostProcessor)} interface:" +
                          $" {string.Join(",", _postProcessors.Where(p => !(p.Processor is ISupportIncrementalPostProcessor)).Select(p => p.ContractName))}.";
            Logger.LogVerbose(message);
            return false;
        }

        // TODO: report incremental info in manifest
        private bool GetCanIncrementalPostProcess()
        {
            if (!ShouldTraceIncrementalInfo)
            {
                return false;
            }
            if (!EnableIncremental)
            {
                const string message = "Disable incremental post processing.";
                Logger.LogVerbose(message);
                return false;
            }
            if (LastInfo == null)
            {
                const string message = "Cannot support incremental post processors, because last post processor info is null.";
                Logger.LogVerbose(message);
                return false;
            }
            if (CurrentInfo.PostProcessorInfos.Count != LastInfo.PostProcessorInfos.Count)
            {
                var message = $"Cannot support incremental post processors, because post processor info count mismatch: last has {LastInfo.PostProcessorInfos.Count} while current has {CurrentInfo.PostProcessorInfos.Count}.";
                Logger.LogVerbose(message);
                return false;
            }
            for (var i = 0; i < CurrentInfo.PostProcessorInfos.Count; i++)
            {
                var currentPostProcessorInfo = CurrentInfo.PostProcessorInfos[i];
                var lastPostProcessorInfo = LastInfo.PostProcessorInfos[i];
                if (!currentPostProcessorInfo.Equals(lastPostProcessorInfo))
                {
                    var message = $"Cannot support incremental post processors, because post processor info changed from last {lastPostProcessorInfo.ToJsonString()} to current {currentPostProcessorInfo.ToJsonString()}.";
                    Logger.LogVerbose(message);
                    return false;
                }
            }

            return true;
        }

        private PostProcessInfo GeneratePostProcessInfo()
        {
            var postProcessInfo = new PostProcessInfo();
            postProcessInfo.PostProcessorInfos.AddRange(_postProcessors.Select(
                postProcessor =>
                    new PostProcessorInfo
                    {
                        Name = postProcessor.ContractName,
                        IncrementalContextHash =
                            ((ISupportIncrementalPostProcessor)postProcessor.Processor).GetIncrementalContextHash(),
                    }).ToList());

            return postProcessInfo;
        }

        #endregion
    }
}
