// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private static readonly ConcurrentDictionary<FilePath, (string, string, string)> s_fileTypeCache =
            new ConcurrentDictionary<FilePath, (string, string, string)>();

        // Set value per dimension limit to int.MaxValue
        // https://github.com/microsoft/ApplicationInsights-dotnet/issues/1496
        private static readonly MetricConfiguration s_metricConfiguration =
            new MetricConfiguration(1000, int.MaxValue, new MetricSeriesConfigurationForMeasurement(false));

        private static readonly Metric s_operationTimeMetric =
            s_telemetryClient.GetMetric(new MetricIdentifier(null, $"Time", "Name", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);

        private static readonly Metric s_errorCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(
                    null, $"BuildLog", "Code", "Level", "Name", "Type", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);

        private static readonly Metric s_buildFileTypeCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(null, "BuildFileType", "FileExtension", "DocumentType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId"),
                s_metricConfiguration);

        private static readonly Metric s_markdownElementCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(
                    null, "MarkdownElement", "ElementType", "FileExtension", "DocumentType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId"),
                s_metricConfiguration);

        private static readonly string s_version =
            typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";

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

        public static void TrackErrorCount(string code, ErrorLevel level, string? name)
        {
            s_errorCountMetric.TrackValue(1, code, level.ToString(), CoalesceEmpty(name), "User", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackBuildFileTypeCount(FilePath filePath, PublishItem publishItem)
        {
            var (fileExtension, documentType, mimeType) = GetFileType(filePath, publishItem.ContentType, publishItem.Mime);
            s_buildFileTypeCountMetric.TrackValue(1, fileExtension, documentType, mimeType, s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackMarkdownElement(Document file, Dictionary<string, int> elementCount)
        {
            var (fileExtension, documentType, mimeType) = GetFileType(file.FilePath, file.ContentType, file.Mime.Value);
            foreach (var (elementType, value) in elementCount)
            {
                s_markdownElementCountMetric.TrackValue(
                    value, CoalesceEmpty(elementType), fileExtension, documentType, mimeType, s_os, s_version, s_repo, s_branch, s_correlationId);
            }
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

        private static (string fileExtension, string documentType, string mimeType) GetFileType(FilePath filePath, ContentType contentType, string? mime)
        {
            return s_fileTypeCache.GetOrAdd(filePath, filePath =>
            {
                var fileExtension = CoalesceEmpty(Path.GetExtension(filePath.Path)?.ToLowerInvariant());
                var mimeType = CoalesceEmpty(mime);
                return (fileExtension, contentType.ToString(), mimeType);
            });
        }

        private static string CoalesceEmpty(string? str)
        {
            return string.IsNullOrEmpty(str) ? "<null>" : str;
        }
    }
}
