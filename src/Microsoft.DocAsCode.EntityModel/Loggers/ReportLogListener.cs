// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.IO;

    using Newtonsoft.Json;

    public sealed class ReportLogListener : ILoggerListener
    {
        private readonly StreamWriter _writer;

        public LogLevel LogLevelThreshold { get; set; }

        public ReportLogListener(string reportPath)
        {
            var dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _writer = new StreamWriter(reportPath, true);
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var level = item.LogLevel;
            var message = item.Message;
            var phase = item.Phase;
            var file = item.File;
            var line = item.Line;
            if (level < LogLevelThreshold) return;

            var reportItem = new ReportItem
            {
                Severity = GetSeverity(level),
                Message = message,
                Source = phase,
                File = file,
                Line = line,
                DateTime = DateTime.UtcNow
            };

            _writer.WriteLine(JsonUtility.Serialize(reportItem));
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private MessageSeverity GetSeverity(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    return MessageSeverity.Verbose;
                case LogLevel.Info:
                    return MessageSeverity.Info;
                case LogLevel.Warning:
                    return MessageSeverity.Warning;
                case LogLevel.Error:
                    return MessageSeverity.Error;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public class ReportItem
        {
            [JsonProperty("message")]
            public string Message { get; set; }
            [JsonProperty("source")]
            public string Source { get; set; }
            [JsonProperty("file")]
            public string File { get; set; }
            [JsonProperty("line")]
            public string Line { get; set; }
            [JsonProperty("date_time")]
            public DateTime DateTime { get; set; }
            [JsonProperty("message_severity")]
            public MessageSeverity Severity { get; set; }
        }

        public enum MessageSeverity
        {
            Error,
            Warning,
            Info,
            Verbose
        }
    }
}
