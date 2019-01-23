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
        }
        public static class FullBuildReason
        {
            public const string NoAvailableBuildCache = "NoAvailableBuildCache";
            public const string DocFxVersionEnforcement = "DocFxVersionEnforcement";            
            public const string PluginChanged = "PluginChanged";
            public const string SHAMismatch = "SHAMismatch";
            public const string NotContainGroup = "NotContainGroup";
            public const string ConfigChanged = "ConfigChanged";
            public const string ForceRebuild = "ForceRebuild";
        }
    }
}
