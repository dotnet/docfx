// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class Telemetry
{
    // https://github.com/microsoft/ApplicationInsights-Home/blob/master/EndpointSpecs/Schemas/Bond/EventData.bond#L19
    private const int MaxEventPropertyLength = 8192;
    private const int MaxChildrenLength = 5;
    private static readonly DependencyTrackingTelemetryModule s_dependencyTrackingTelemetryModule = new();
    private static readonly TelemetryConfiguration s_telemetryConfiguration = GetTelemetryConfiguration();
    private static readonly TelemetryClient s_telemetryClient = new(s_telemetryConfiguration);

    private static TelemetryConfiguration GetTelemetryConfiguration()
    {
        var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
        telemetryConfiguration.TelemetryInitializers.Add(new DependencyTelemetryInitializer());
        s_dependencyTrackingTelemetryModule.Initialize(telemetryConfiguration);
        return telemetryConfiguration;
    }

    // Set value per dimension limit to int.MaxValue
    // https://github.com/microsoft/ApplicationInsights-dotnet/issues/1496
    private static readonly MetricConfiguration s_metricConfiguration = new(int.MaxValue, int.MaxValue, new MetricSeriesConfigurationForMeasurement(false));

    private static readonly Metric s_operationStartMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(null, "OperationStart", "Name", "OS", "Version", "Repo", "Branch", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_operationEndMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(null, "OperationEnd", "Name", "OS", "Version", "Repo", "Branch", "TimeBucket", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_errorCountMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(
                null, "BuildLog", "Code", "Level", "Name", "AdditionalErrorInfo", "OS", "Version", "Repo", "Branch", "CorrelationId", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_fileLogCountMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(
                null, "BuildFileLogCount", "Level", "File", "OS", "Version", "Repo", "Branch", "CorrelationId", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_buildFileTypeCountMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(
                null, "BuildFileType", "FileExtension", "DocumentType", "MimeType", "OS", "Version", "Repo", "Branch", "CorrelationId", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_githubRateLimitMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(null, "GitHubRateLimit", "Remaining", "OS", "Version", "Repo", "Branch", "CorrelationId", "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_markdownElementCountMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(
                null,
                "MarkdownElement",
                "ElementType",
                "FileExtension",
                "DocumentType",
                "MimeType",
                "OS",
                "Version",
                "Repo",
                "Branch",
                "CorrelationId",
                "SessionId"),
            s_metricConfiguration);

    private static readonly Metric s_htmlElementCountMetric =
        s_telemetryClient.GetMetric(
            new MetricIdentifier(
                null,
                "HtmlElement",
                "ElementType",
                "IsAllowed",
                "FileExtension",
                "DocumentType",
                "MimeType",
                "Repo",
                "Branch",
                "CorrelationId"),
            s_metricConfiguration);

    private static readonly string s_version =
        typeof(Telemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "<null>";

    private static readonly string s_os = RuntimeInformation.OSDescription ?? "<null>";
    private static readonly string s_correlationId = EnvironmentVariable.CorrelationId ?? Guid.NewGuid().ToString("N");
    private static readonly string s_sessionId = EnvironmentVariable.SessionId ?? "<null>";
    private static readonly AsyncLocal<bool> s_isRealTimeBuild = new();

    private static string s_repo = "<null>";
    private static string s_branch = "<null>";

    public static void SetRepository(string? repo, string? branch)
    {
        s_repo = CoalesceEmpty(repo);
        s_branch = CoalesceEmpty(branch);
    }

    public static void SetIsRealTimeBuild(bool isRealTimeBuild)
    {
        s_isRealTimeBuild.Value = isRealTimeBuild;
    }

    public static void TrackDocfxConfig(string docsetName, JObject docfxConfig)
    {
        if (!s_isRealTimeBuild.Value)
        {
            var docfxConfigTelemetryValue = JsonUtility.Serialize(docfxConfig);
            var hashCode = HashUtility.GetSha256Hash(docfxConfigTelemetryValue);
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
    }

    public static DelegatingCompletable StartOperation(string name)
    {
        var stopwatch = Stopwatch.StartNew();
        TrackValueWithEnsurance(
                s_operationStartMetric.Identifier.MetricId,
                s_operationStartMetric.TrackValue(1, name, s_os, s_version, s_repo, s_branch, s_sessionId));
        return new DelegatingCompletable(() =>
        {
            Log.Important($"{name} done in {Progress.FormatTimeSpan(stopwatch.Elapsed)}", ConsoleColor.Green);
            TrackValueWithEnsurance(
                s_operationEndMetric.Identifier.MetricId,
                s_operationEndMetric.TrackValue(
                stopwatch.ElapsedMilliseconds, name, s_os, s_version, s_repo, s_branch, GetTimeBucket(stopwatch.Elapsed), s_sessionId));
        });
    }

    public static void TrackErrorCount(Error error)
    {
        var code = error.Code;
        var level = error.Level;
        var name = error.PropertyPath;
        var additionalErrorInfoString = error.AdditionalErrorInfo == null ? "{}" : JsonUtility.Serialize(error.AdditionalErrorInfo);
        if (!s_isRealTimeBuild.Value)
        {
            TrackValueWithEnsurance(
                s_errorCountMetric.Identifier.MetricId,
                s_errorCountMetric.TrackValue(
                    1,
                    code,
                    level.ToString(),
                    CoalesceEmpty(name),
                    additionalErrorInfoString,
                    s_os,
                    s_version,
                    s_repo,
                    s_branch,
                    s_correlationId,
                    s_sessionId));
        }
    }

    public static void TrackFileLogCount(ErrorLevel level, FilePath? filePath)
    {
        if (!s_isRealTimeBuild.Value && filePath != null)
        {
            TrackValueWithEnsurance(
                s_fileLogCountMetric.Identifier.MetricId,
                s_fileLogCountMetric.TrackValue(
                1, level.ToString(), CoalesceEmpty(filePath.ToString()), s_os, s_version, s_repo, s_branch, s_correlationId, s_sessionId));
        }
    }

    public static void TrackGitHubRateLimit(string? remaining)
    {
        if (!s_isRealTimeBuild.Value)
        {
            TrackValueWithEnsurance(
                s_githubRateLimitMetric.Identifier.MetricId,
                s_githubRateLimitMetric.TrackValue(1, CoalesceEmpty(remaining), s_os, s_version, s_repo, s_branch, s_correlationId, s_sessionId));
        }
    }

    public static void TrackBuildFileTypeCount(FilePath file, ContentType contentType, string? mime)
    {
        if (!s_isRealTimeBuild.Value)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.Path)?.ToLowerInvariant());

            TrackValueWithEnsurance(
                s_buildFileTypeCountMetric.Identifier.MetricId,
                s_buildFileTypeCountMetric.TrackValue(
                    1, fileExtension, contentType.ToString(), CoalesceEmpty(mime), s_os, s_version, s_repo, s_branch, s_correlationId, s_sessionId));
        }
    }

    public static void TrackMarkdownElement(FilePath file, ContentType contentType, string? mime, Dictionary<string, int> elementCount)
    {
        if (!s_isRealTimeBuild.Value)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.Path)?.ToLowerInvariant());
            var documentType = contentType.ToString();
            var mimeType = CoalesceEmpty(mime);

            foreach (var (elementType, value) in elementCount)
            {
                TrackValueWithEnsurance(
                    s_markdownElementCountMetric.Identifier.MetricId,
                    s_markdownElementCountMetric.TrackValue(
                        value,
                        CoalesceEmpty(elementType),
                        fileExtension,
                        documentType,
                        mimeType,
                        s_os,
                        s_version,
                        s_repo,
                        s_branch,
                        s_correlationId,
                        s_sessionId));
            }
        }
    }

    public static void TrackHtmlElement(
        FilePath file,
        ContentType contentType,
        string? mime,
        Dictionary<string, Dictionary<string, int>> elementCount,
        Func<string, string, bool> isAllowed)
    {
        if (!s_isRealTimeBuild.Value)
        {
            var fileExtension = CoalesceEmpty(Path.GetExtension(file.Path)?.ToLowerInvariant());
            var documentType = contentType.ToString();
            var mimeType = CoalesceEmpty(mime);

            foreach (var (tokenName, attributeCount) in elementCount)
            {
                foreach (var (attributeName, count) in attributeCount)
                {
                    var elementType = string.IsNullOrEmpty(attributeName) ? tokenName : $"{tokenName}_{attributeName}";
                    TrackValueWithEnsurance(
                    s_htmlElementCountMetric.Identifier.MetricId,
                    s_htmlElementCountMetric.TrackValue(
                    count,
                    CoalesceEmpty(elementType),
                    isAllowed(tokenName, attributeName).ToString(),
                    fileExtension,
                    documentType,
                    mimeType,
                    s_repo,
                    s_branch,
                    s_correlationId));
                }
            }
        }
    }

    public static void TrackException(Exception ex)
    {
        s_telemetryClient.TrackException(ex);
    }

    public static void Flush()
    {
        // TelemetryClient.Flush may meet deadlocks: https://github.com/microsoft/ApplicationInsights-dotnet/issues/1186
        Task.WaitAny(Task.Run(s_telemetryClient.Flush), Task.Delay(10000));
    }

    public static IOperationHolder<RequestTelemetry> StartOperation()
    {
        var operation = s_telemetryClient.StartOperation<RequestTelemetry>("docfx");
        operation.Telemetry.Properties.Add("CorrelationId", s_correlationId);
        return operation;
    }

    private static void TrackValueWithEnsurance(string metricsName, bool trackValueResult)
    {
        if (!trackValueResult)
        {
            Log.Write($"Track value for {metricsName} fails.");
        }
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

        eventTelemetry.Properties["OS"] = s_os;
        eventTelemetry.Properties["Version"] = s_version;
        eventTelemetry.Properties["Repo"] = s_repo;
        eventTelemetry.Properties["Branch"] = s_branch;
        eventTelemetry.Properties["CorrelationId"] = s_correlationId;
        eventTelemetry.Properties["SessionId"] = s_sessionId;

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

    private static string GetTimeBucket(TimeSpan value)
        => value.TotalSeconds switch
        {
            < 0.5 => "small",
            < 20 => "middle",
            _ => "large",
        };

    private class DependencyTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is DependencyTelemetry dependencyTelemetry)
            {
                dependencyTelemetry.Data = UrlUtility.SanitizeUrl(dependencyTelemetry.Data);
                dependencyTelemetry.Properties["CorrelationId"] = s_correlationId;
            }
        }
    }
}
