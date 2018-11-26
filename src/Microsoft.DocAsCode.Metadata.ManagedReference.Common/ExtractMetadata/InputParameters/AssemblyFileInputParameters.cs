// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class AssemblyFileInputParameters : IInputParameters
    {
        public ExtractMetadataOptions Options { get; }

        public IEnumerable<string> Files { get; set; }

        public string Key { get; }

        public ProjectLevelCache Cache { get; }

        public BuildInfo BuildInfo { get; }

        public AssemblyFileInputParameters(ExtractMetadataOptions options, string key)
        {
            Options = options;
            Files = new string[] { key };
            Key = StringExtension.GetNormalizedFullPathKey(Files);
            Cache = ProjectLevelCache.Get(key);
            BuildInfo = Cache?.GetValidConfig(Key);
        }

        public bool HasChanged(BuildInfo buildInfo)
        {
            var check = new IncrementalCheck(buildInfo);

            return Options.HasChanged(check, false) || check.AreFilesModified(Files);
        }
    }
}
