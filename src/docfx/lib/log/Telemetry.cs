// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        private static readonly TelemetryClient s_telemetryClient = new TelemetryClient(TelemetryConfiguration.CreateDefault());

        private static readonly Metric s_operationTimeMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Time", "Name", "OS", "Version", "Repo", "Branch", "CorrelationId"));
        private static readonly Metric s_errorCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"BuildLog", "Code", "Level", "Type", "OS", "Version", "Repo", "Branch", "CorrelationId"));
        private static readonly Metric s_cacheCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Cache", "Name", "State", "OS", "Version", "Repo", "Branch", "CorrelationId"));
        private static readonly Metric s_buildItemCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Count", "Name", "OS", "Version", "Repo", "Branch", "CorrelationId"));

        private static readonly string s_version = typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";
        private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";

        private static string s_repo = "<null>";
        private static string s_branch = "<null>";

        private static Dictionary<string, string> s_eventDimensions = new Dictionary<string, string>();
        private static string s_correlationId = Guid.NewGuid().ToString("N");

        public static void SetRepository(string repo, string branch)
        {
            s_repo = string.IsNullOrEmpty(repo) ? "<null>" : repo;
            s_branch = string.IsNullOrEmpty(branch) ? "<null>" : branch;
        }

        public static void SetTelemetryConfig(TelemetryConfig telemetryConfig)
        {
            if (!string.IsNullOrEmpty(telemetryConfig.CorrelationId))
            {
                s_correlationId = telemetryConfig.CorrelationId;
            }
            s_eventDimensions = telemetryConfig.EventDimensions;
        }

        public static void TrackOperationTime(string name, TimeSpan duration)
        {
            s_operationTimeMetric.TrackValue(duration.TotalMilliseconds, name, s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static IDisposable TrackingOperationTime(TelemetryName name)
        {
            return new PerfScope(name.ToString());
        }

        public static void TrackErrorCount(string code, ErrorLevel level)
        {
            s_errorCountMetric.TrackValue(1, code, level.ToString(), "User", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackCacheTotalCount(TelemetryName name)
        {
            s_cacheCountMetric.TrackValue(1, name.ToString(), "total", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackCacheMissCount(TelemetryName name)
        {
            s_cacheCountMetric.TrackValue(1, name.ToString(), "miss", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackBuildItemCount(ContentType contentType)
        {
            s_buildItemCountMetric.TrackValue(1, $"{TelemetryName.BuildItems}-{contentType}", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackBuildCommitCount(int count)
        {
            s_buildItemCountMetric.TrackValue(count, TelemetryName.BuildCommits.ToString(), s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackException(Exception ex)
        {
            s_telemetryClient.TrackException(ex, new Dictionary<string, string>
            {
                { "OS", s_os },
                { "Version", s_version },
                { "Repo", s_repo },
                { "Branch", s_branch },
                { "CorrelationId", s_correlationId },
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
