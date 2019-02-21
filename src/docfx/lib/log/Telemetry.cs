// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        private static readonly TelemetryClient s_telemetryClient = new TelemetryClient();

        private static readonly string s_version = typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";
        private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";

        private static string s_repo = "<null>";
        private static string s_branch = "<null>";

        public static void SetRepository(string repo, string branch)
        {
            s_repo = repo ?? "<null>";
            s_branch = branch ?? "<null>";
        }

        public static void TrackOperationDuration(string name, TimeSpan duration)
        {
            s_telemetryClient
                .GetMetric(new MetricIdentifier(null, $"time", "name", "os", "version", "repo", "branch"))
                .TrackValue(duration.TotalMilliseconds, name, s_os, s_version, s_repo, s_branch);
        }

        public static void TrackErrorCount(string code, ErrorLevel level)
        {
            s_telemetryClient
                .GetMetric(new MetricIdentifier(null, $"error", "code", "level", "os", "version", "repo", "branch"))
                .TrackValue(1, code, level.ToString(), s_os, s_version, s_repo, s_branch);
        }

        public static void TrackCacheTotal(CacheName name)
        {
            s_telemetryClient
                .GetMetric(new MetricIdentifier(null, $"cache", "name", "state", "os", "version", "repo", "branch"))
                .TrackValue(1, name.ToString(), "total", s_os, s_version, s_repo, s_branch);
        }

        public static void TrackCacheMiss(CacheName name)
        {
            s_telemetryClient
                .GetMetric(new MetricIdentifier(null, $"cache", "name", "state", "os", "version", "repo", "branch"))
                .TrackValue(1, name.ToString(), "miss", s_os, s_version, s_repo, s_branch);
        }

        public static void TrackException(Exception ex)
        {
            s_telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "os", s_os },
                { "version", s_version },
                { "repo", s_repo },
                { "branch", s_branch },
            });
        }

        public static void Flush()
        {
            // Default timeout of 100 sec is used
            s_telemetryClient.Flush();
        }
    }
}
