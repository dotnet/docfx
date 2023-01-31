// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class ProjectFileInputParameters : IInputParameters
    {
        public ExtractMetadataOptions Options { get; }

        public IEnumerable<string> Files { get; set; }

        public bool DependencyRebuilt { get; set; }

        public string Key { get; }

        public ProjectLevelCache Cache { get; }

        public BuildInfo BuildInfo { get; }

        public ProjectFileInputParameters(ExtractMetadataOptions options, IEnumerable<string> files, string projectFile, bool dependencyRebuilt)
        {
            Options = options;
            Files = files;
            DependencyRebuilt = dependencyRebuilt;
            Key = StringExtension.ToNormalizedFullPath(projectFile);
            Cache = ProjectLevelCache.Get(projectFile);
            BuildInfo = Cache?.GetValidConfig(Key);
        }

        public bool HasChanged(BuildInfo buildInfo)
        {
            var check = new IncrementalCheck(buildInfo);

            return DependencyRebuilt || Options.HasChanged(check, true) || check.AreFilesModified(Files);
        }
    }
}
