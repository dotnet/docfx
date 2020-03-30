// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
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

        // Set value per dimension limit to int.MaxValue
        // https://github.com/microsoft/ApplicationInsights-dotnet/issues/1496
        private static readonly MetricConfiguration s_metricConfiguration = new MetricConfiguration(1000, int.MaxValue, new MetricSeriesConfigurationForMeasurement(false));

        private static readonly Metric s_operationTimeMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Time", "Name", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);
        private static readonly Metric s_errorCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"BuildLog", "Code", "Level", "Type", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);
        private static readonly Metric s_cacheCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Cache", "Name", "State", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);
        private static readonly Metric s_buildCommitCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, $"BuildCommitCount", "Name", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);
        private static readonly Metric s_buildFileTypeCountMetric = s_telemetryClient.GetMetric(new MetricIdentifier(null, "BuildFileType", "FileExtension", "DocuemntType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);

        private static readonly string s_version = typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";
        private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";

        private static string s_repo = "<null>";
        private static string s_branch = "<null>";

        private static string s_correlationId = EnvironmentVariable.CorrelationId ?? Guid.NewGuid().ToString("N");

        static Telemetry()
        {
            s_telemetryClient.Context.GlobalProperties["OS"] = s_os;
            s_telemetryClient.Context.GlobalProperties["Version"] = s_version;
            s_telemetryClient.Context.GlobalProperties["CorrelationId"] = s_correlationId;
        }

        public static void SetRepository(string? repo, string? branch)
        {
            s_repo = CoalesceEmpty(repo);
            s_branch = CoalesceEmpty(branch);
            s_telemetryClient.Context.GlobalProperties["Repo"] = s_repo;
            s_telemetryClient.Context.GlobalProperties["Branch"] = s_branch;
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

        public static void TrackBuildFileTypeCount(Document file)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.FilePath.Path)?.ToLowerInvariant());
            var mimeType = CoalesceEmpty(file.Mime.Value);
            if (mimeType == "<null>" && file.ContentType == ContentType.Page && fileExtension == ".md")
            {
                mimeType = "Conceptual";
            }
            s_buildFileTypeCountMetric.TrackValue(1, fileExtension, file.ContentType.ToString(), mimeType, s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackBuildCommitCount(int count)
        {
            s_buildCommitCountMetric.TrackValue(count, TelemetryName.BuildCommits.ToString(), s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackException(Exception ex)
        {
            s_telemetryClient.TrackException(ex);
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

        private static string CoalesceEmpty(string? str)
        {
            return string.IsNullOrEmpty(str) ? "<null>" : str;
        }
    }
}
