// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Telemetry
    {
        // https://github.com/microsoft/ApplicationInsights-Home/blob/master/EndpointSpecs/Schemas/Bond/EventData.bond#L19
        private const int MaxEventPropertyLength = 8192;
        private const int MaxChildrenLength = 5;
        private static readonly TelemetryClient s_telemetryClient = new TelemetryClient(TelemetryConfiguration.CreateDefault());

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

        private static readonly Metric s_fileLogCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(
                    null, $"BuildFileLogCount", "Level", "File", "OS", "Version", "Repo", "Branch", "CorrelationId"), s_metricConfiguration);

        private static readonly Metric s_buildFileTypeCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(null, "BuildFileType", "FileExtension", "DocumentType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId"),
                s_metricConfiguration);

        private static readonly Metric s_githubRateLimitMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(null, "GitHubRateLimit", "Remaining", "OS", "Version", "Repo", "Branch", "CorrelationId"),
                s_metricConfiguration);

        private static readonly Metric s_markdownElementCountMetric =
            s_telemetryClient.GetMetric(
                new MetricIdentifier(
                    null, "MarkdownElement", "ElementType", "FileExtension", "DocumentType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId"),
                s_metricConfiguration);

        private static readonly string s_version =
            typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";

        private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";
        private static readonly string s_correlationId = EnvironmentVariable.CorrelationId ?? Guid.NewGuid().ToString("N");

        private static string s_repo = "<null>";
        private static string s_branch = "<null>";

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

        public static void TrackDocfxConfig(string docsetName, JObject docfxConfig)
        {
            var docfxConfigTelemetryValue = JsonUtility.Serialize(docfxConfig);
            var hashCode = HashUtility.GetMd5Hash(docfxConfigTelemetryValue);
            if (docfxConfigTelemetryValue.Length > MaxEventPropertyLength)
            {
                var newValue = JsonUtility.DeepClone(docfxConfig) as JObject;
                TryRemoveNestedObject(newValue!);
                docfxConfigTelemetryValue = JsonUtility.Serialize(newValue!);
            }

            var properties = new Dictionary<string, string>
            {
                ["DocsetName"] = docsetName,
                ["Config"] = docfxConfigTelemetryValue,
                ["ContentHash"] = hashCode,
            };

            TrackEvent("docfx.json", properties);
        }

        public static void TrackOperationTime(string name, TimeSpan duration)
        {
            s_operationTimeMetric.TrackValue(duration.TotalMilliseconds, name, s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackErrorCount(string code, ErrorLevel level, string? name)
        {
            s_errorCountMetric.TrackValue(1, code, level.ToString(), CoalesceEmpty(name), "User", s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackFileLogCount(ErrorLevel level, FilePath? filePath)
        {
            if (filePath != null)
            {
                s_fileLogCountMetric.TrackValue(1, level.ToString(), CoalesceEmpty(filePath.ToString()), s_os, s_version, s_repo, s_branch, s_correlationId);
            }
        }

        public static void TrackGitHubRateLimit(string? remaining)
        {
            s_githubRateLimitMetric.TrackValue(1, CoalesceEmpty(remaining), s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackBuildFileTypeCount(FilePath file, ContentType contentType, string? mime)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.Path)?.ToLowerInvariant());

            s_buildFileTypeCountMetric.TrackValue(
                1, fileExtension, contentType.ToString(), CoalesceEmpty(mime), s_os, s_version, s_repo, s_branch, s_correlationId);
        }

        public static void TrackMarkdownElement(FilePath file, ContentType contentType, string? mime, Dictionary<string, int> elementCount)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.Path)?.ToLowerInvariant());
            var documentType = contentType.ToString();
            var mimeType = CoalesceEmpty(mime);

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

        private static void TrackEvent(string name, IReadOnlyDictionary<string, string> properties)
        {
            var eventTelemetry = new EventTelemetry
            {
                Name = name,
            };

            foreach (var property in properties)
            {
                eventTelemetry.Properties[property.Key] = property.Value;
            }

            s_telemetryClient.TrackEvent(eventTelemetry);
        }

        private static string CoalesceEmpty(string? str)
        {
            return string.IsNullOrEmpty(str) ? "<null>" : str;
        }

        private static void TryRemoveNestedObject(this JObject graph)
        {
            foreach (var (key, value) in graph)
            {
                if (value is JObject propertyValue)
                {
                    foreach (var (nestedKey, nestedValue) in propertyValue)
                    {
                        if (nestedValue is JObject @object && @object.Count > MaxChildrenLength)
                        {
                            propertyValue[nestedKey] = new JObject();
                        }
                        if (nestedValue is JArray array && array.Count > MaxChildrenLength)
                        {
                            propertyValue[nestedKey] = new JArray();
                        }
                    }
                }
            }
        }
    }
}
