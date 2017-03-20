// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;
    using System.IO;

    public interface ICanTraceContextInfoBuildStep : ISupportIncrementalBuildStep
    {
        void SaveContext(TraceContext traceContext, StreamWriter writer);

        void LoadFromContext(TraceContext traceContext, StreamReader reader);
    }

    public class TraceContext
    {
        public IReadOnlyList<FileIncrementalInfo> AllSourceFileInfo { get; }

        public BuildPhase Phase { get; }

        public TraceContext(List<FileIncrementalInfo> sourceFiles, BuildPhase phase)
        {
            AllSourceFileInfo = sourceFiles;
            Phase = phase;
        }
    }

    public class FileIncrementalInfo
    {
        public string SourceFile { get; set; }

        public bool IsIncremental { get; set; }
    }
}
