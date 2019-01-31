// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    public static class EnvironmentVariable
    {
        public static string GlobalConfigPath => Environment.GetEnvironmentVariable("DOCFX_GLOBAL_CONFIG_PATH");

        public static string AppDataPath => Environment.GetEnvironmentVariable("DOCFX_APPDATA_PATH");

        public static string RepositoryUrl => Environment.GetEnvironmentVariable("DOCFX_REPOSITORY_URL");

        public static string RepositoryBranch => Environment.GetEnvironmentVariable("DOCFX_REPOSITORY_BRANCH");
    }
}
