// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Docfx.Common;

public sealed class ReportLogListener : ILoggerListener
{
    private readonly string _repoRoot;
    private readonly string _root;
    private readonly StreamWriter _writer;

    private const LogLevel LogLevelThreshold = LogLevel.Diagnostic;

    public ReportLogListener(string reportPath, string repoRoot, string root)
    {
        var dir = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _writer = new StreamWriter(reportPath, true);
        _repoRoot = repoRoot;
        _root = root;
    }

    public void WriteLine(ILogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var level = item.LogLevel;
        var message = item.Message;
        var file = item.File;
        var line = item.Line;
        if (level < LogLevelThreshold) return;

        var reportItem = new ReportItem
        {
            Severity = GetSeverity(level),
            Message = message,
            File = TransformFile(file),
            Line = line,
            DateTime = DateTime.UtcNow,
            Code = item.Code,
        };

        _writer.WriteLine(JsonUtility.Serialize(reportItem));
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    private static MessageSeverity GetSeverity(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Diagnostic:
                return MessageSeverity.Diagnostic;
            case LogLevel.Verbose:
                return MessageSeverity.Verbose;
            case LogLevel.Info:
                return MessageSeverity.Info;
            case LogLevel.Suggestion:
                return MessageSeverity.Suggestion;
            case LogLevel.Warning:
                return MessageSeverity.Warning;
            case LogLevel.Error:
                return MessageSeverity.Error;
            default:
                throw new NotSupportedException(level.ToString());
        }
    }

    private string TransformFile(string fileFromRoot)
    {
        if (fileFromRoot == null)
        {
            return null;
        }
        if (string.IsNullOrEmpty(_repoRoot))
        {
            return fileFromRoot;
        }

        string file = ((RelativePath)fileFromRoot).RemoveWorkingFolder();
        string basePath = Path.GetFullPath(_repoRoot);
        string fullPath = Path.GetFullPath(Path.Combine(_root, file));
        return PathUtility.MakeRelativePath(basePath, fullPath);
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public class ReportItem
    {
        [JsonProperty("message")]
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonProperty("file")]
        [JsonPropertyName("file")]
        public string File { get; set; }

        [JsonProperty("line")]
        [JsonPropertyName("line")]
        public string Line { get; set; }

        [JsonProperty("date_time")]
        [JsonPropertyName("date_time")]
        public DateTime DateTime { get; set; }

        [JsonProperty("message_severity")]
        [JsonPropertyName("message_severity")]
        public MessageSeverity Severity { get; set; }

        [JsonProperty("code")]
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public enum MessageSeverity
    {
        Error,
        Warning,
        Suggestion,
        Info,
        Verbose,
        Diagnostic
    }
}
