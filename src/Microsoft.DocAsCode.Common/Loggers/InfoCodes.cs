// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    public static class InfoCodes
    {
        public static class Build
        {
            public const string IsFullBuild = "IsFullBuild";
            public const string IsIncrementalBuild = "IsIncrementalBuild";
            public const string MarkdownEngineName = "MarkdownEngineName";
        }
        public static class FullBuildReason
        {
            public const string NoAvailableBuildCache = "NoAvailableBuildCache";
            public const string DocfxVersionChanged = "DocfxVersionChanged";
            public const string PluginChanged = "PluginChanged";
            public const string CommitShaMismatch = "CommitShaMismatch";
            public const string NoAvailableGroupCache = "NoAvailableGroupCache";
            public const string ConfigChanged = "ConfigChanged";
            public const string ForceRebuild = "ForceRebuild";
        }

        public static class IncrementalBuildReason
        {
            public const string TemplateChanged = "TemplateChanged";
        }
    }
}
