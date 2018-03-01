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

        public BuildInfo CurrentBuildInfo { get; }

        public PostProcessInfo CurrentInfo { get; }

        public PostProcessInfo LastInfo { get; }

        public bool EnableIncremental { get; }

        public string CurrentBaseDir { get; }

        public string LastBaseDir { get; }

        public int MaxParallelism { get; }

        public IncrementalInfo IncrementalInfo { get; } = new IncrementalInfo();

        /// <summary>
        /// Whether to trace the incremental info in intermediate folder
        /// </summary>
        public bool ShouldTraceIncrementalInfo { get; }

        /// <summary>
        /// Whether to post process incrementally
        /// </summary>
        public bool IsIncremental { get; }

        #endregion

        #region Constructor

        public IncrementalPostProcessorsContext(
            string intermediateFolder,
            BuildInfo currentBuildInfo,
            BuildInfo lastBuildInfo,
            List<PostProcessor> postProcessors,
            bool enableIncremental,
            int maxParallelism)
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
            if (maxParallelism <= 0)
            {
                maxParallelism = Environment.ProcessorCount;
            }

            _postProcessors = postProcessors;

            ShouldTraceIncrementalInfo = GetShouldTraceIncrementalInfo();
            if (ShouldTraceIncrementalInfo)
            {
                currentBuildInfo.PostProcessInfo = GeneratePostProcessInfo();
            }
            CurrentBuildInfo = currentBuildInfo;
            CurrentInfo = currentBuildInfo.PostProcessInfo;
            LastInfo = lastBuildInfo?.PostProcessInfo;
            CurrentBaseDir = Path.Combine(intermediateFolder, currentBuildInfo.DirectoryName);
            LastBaseDir = lastBuildInfo == null ? null : Path.Combine(intermediateFolder, lastBuildInfo.DirectoryName);
            EnableIncremental = enableIncremental;
            IsIncremental = GetIsIncremental();
            MaxParallelism = maxParallelism;
        }

        #endregion

        #region Private methods

        private bool GetShouldTraceIncrementalInfo()
        {
            if (_postProcessors.All(p => p.Processor is ISupportIncrementalPostProcessor))
            {
                Logger.LogVerbose("Should trace post processing incremental info, because all post processors support incremental.");
                return true;
            }

            var message = $"Cannot support incremental post processors, because following post processors don't implement {nameof(ISupportIncrementalPostProcessor)} interface:" +
                          $" {string.Join(",", _postProcessors.Where(p => !(p.Processor is ISupportIncrementalPostProcessor)).Select(p => p.ContractName))}.";
            Logger.LogVerbose(message);
            return false;
        }

        private bool GetIsIncremental()
        {
            const string prependWarning = "Cannot support incremental post processing, the reason is:";
            string message;
            if (!ShouldTraceIncrementalInfo)
            {
                message = $"{prependWarning} should not trace intermediate info.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.PostProcessing, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (!EnableIncremental)
            {
                message = $"{prependWarning} it's disabled.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.PostProcessing, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (LastInfo == null)
            {
                message = $"{prependWarning} last post processor info is null.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.PostProcessing, message);
                Logger.LogVerbose(message);
                return false;
            }
            if (CurrentInfo.PostProcessorInfos.Count != LastInfo.PostProcessorInfos.Count)
            {
                message = $"{prependWarning} post processor info count mismatch: last has {LastInfo.PostProcessorInfos.Count} while current has {CurrentInfo.PostProcessorInfos.Count}.";
                IncrementalInfo.ReportStatus(false, IncrementalPhase.PostProcessing, message);
                Logger.LogVerbose(message);
                return false;
            }
            for (var i = 0; i < CurrentInfo.PostProcessorInfos.Count; i++)
            {
                var currentPostProcessorInfo = CurrentInfo.PostProcessorInfos[i];
                var lastPostProcessorInfo = LastInfo.PostProcessorInfos[i];
                if (!currentPostProcessorInfo.Equals(lastPostProcessorInfo))
                {
                    message = $"{prependWarning} post processor info changed from last {lastPostProcessorInfo.ToJsonString()} to current {currentPostProcessorInfo.ToJsonString()}.";
                    IncrementalInfo.ReportStatus(false, IncrementalPhase.PostProcessing, message);
                    Logger.LogVerbose(message);
                    return false;
                }
            }

            message = "Can support incremental post processing.";
            IncrementalInfo.ReportStatus(true, IncrementalPhase.PostProcessing, message);
            Logger.LogVerbose(message);
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
