// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        private static readonly TelemetryClient s_telemetryClient = new TelemetryClient();

        private static readonly Metric s_operationTimeMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"time", "name", "os", "version", "repo", "branch"));
        private static readonly Metric s_errorCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"error", "code", "level", "os", "version", "repo", "branch"));
        private static readonly Metric s_cacheCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"cache", "name", "state", "os", "version", "repo", "branch"));
        private static readonly Metric s_buildItemCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"item", "name", "type", "os", "version", "repo", "branch"));
        private static readonly Metric s_commitCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"commit", "name", "os", "version", "repo", "branch"));

        private static readonly string s_version = typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";
        private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";

        private static string s_repo = "<null>";
        private static string s_branch = "<null>";

        public static void SetRepository(string repo, string branch)
        {
            s_repo = repo ?? "<null>";
            s_branch = branch ?? "<null>";
        }

        public static void TrackOperationTime(string name, TimeSpan duration)
        {
            s_operationTimeMetric.TrackValue(duration.TotalMilliseconds, name, s_os, s_version, s_repo, s_branch);
        }

        public static IDisposable TrackingOperationTime(TelemetryName name)
        {
            return new PerfScope(name.ToString());
        }

        public static void TrackErrorCount(string code, ErrorLevel level)
        {
            s_errorCountMetric.TrackValue(1, code, level.ToString(), s_os, s_version, s_repo, s_branch);
        }

        public static void TrackCacheTotalCount(TelemetryName name)
        {
            s_cacheCountMetric.TrackValue(1, name.ToString(), "total", s_os, s_version, s_repo, s_branch);
        }

        public static void TrackCacheMissCount(TelemetryName name)
        {
            s_cacheCountMetric.TrackValue(1, name.ToString(), "miss", s_os, s_version, s_repo, s_branch);
        }

        public static void TrackBuildItemCount(ContentType contentType, int count)
        {
            s_buildItemCountMetric.TrackValue(count, TelemetryName.BuildItems.ToString(), contentType.ToString(), s_os, s_version, s_repo, s_branch);
        }

        public static void TrackBuildCommitCount(int count)
        {
            s_commitCountMetric.TrackValue(count, TelemetryName.BuildCommits.ToString(), s_os, s_version, s_repo, s_branch);
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
            // Default timeout of TelemetryClient.Flush is 100 seconds,
            // but we only want to wait for 2 seconds at most.
            Task.WaitAny(Task.Run(s_telemetryClient.Flush), Task.Delay(2000));
        }

        private class PerfScope : IDisposable
        {
            private readonly string _name;
            private Stopwatch _stopwatch;

            public PerfScope(string name)
            {
                _name = name;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                TrackOperationTime(_name, _stopwatch.Elapsed);
            }
        }
    }
}
