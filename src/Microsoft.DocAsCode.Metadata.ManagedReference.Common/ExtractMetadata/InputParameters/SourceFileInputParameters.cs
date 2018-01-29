// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class SourceFileInputParameters : IInputParameters
    {
        public ExtractMetadataOptions Options { get; }

        public IEnumerable<string> Files { get; }

        public string Key { get; } 

        public ProjectLevelCache Cache { get; }

        public BuildInfo BuildInfo { get; }

        public SourceFileInputParameters(ExtractMetadataOptions options, IEnumerable<string> files)
        {
            Options = options;
            Files = files;
            Key = StringExtension.GetNormalizedFullPathKey(Files);
            Cache = ProjectLevelCache.Get(files);
            BuildInfo = Cache?.GetValidConfig(Key);
        }

        public bool HasChanged(BuildInfo buildInfo)
        {
            var check = new IncrementalCheck(buildInfo);

            return Options.HasChanged(check, true) || check.AreFilesModified(Files);
        }
    }
}
