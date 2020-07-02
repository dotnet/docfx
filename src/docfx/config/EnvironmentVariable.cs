// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    public static class EnvironmentVariable
    {
        public static string? AppDataPath => GetValue("DOCFX_APPDATA_PATH");

        public static string? CachePath => GetValue("DOCFX_CACHE_PATH");

        public static string? StatePath => GetValue("DOCFX_STATE_PATH");

        public static string? RepositoryUrl => GetValue("DOCFX_REPOSITORY_URL");

        public static string? RepositoryBranch => GetValue("DOCFX_REPOSITORY_BRANCH");

        public static string? CorrelationId => GetValue("DOCFX_CORRELATION_ID");

        public static bool UpdateCacheSync { get; } = bool.TryParse(GetValue("DOCFX_UPDATE_CACHE_SYNC"), out bool updateCacheSync) && updateCacheSync;

        private static string? GetValue(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
