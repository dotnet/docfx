// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    public sealed class HtmlLogListener : ILoggerListener
    {
        private readonly StreamWriter _writer;

        private const LogLevel LogLevelThreshold = LogLevel.Diagnostic;

        private static readonly Regex EscapeWithEncode = new Regex("&", RegexOptions.Compiled);
        private static readonly Regex EscapeWithoutEncode = new Regex(@"&(?!#?\w+;)", RegexOptions.Compiled);

#if !NetCore
        public HtmlLogListener(string reportPath)
        {
            var dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _writer = new StreamWriter(reportPath, true);
            WriteCommonHeader();
        }
#endif

        public HtmlLogListener(StreamWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            WriteCommonHeader();
        }

        public void WriteCommonHeader()
        {
            _writer.WriteLine("<html>");
            _writer.WriteLine(@"<body lang=""en-us"">");
            _writer.WriteLine(@"<h1 id=""Report"">Report</h1>");
            _writer.WriteLine(@"<table border=""1"">");
            _writer.WriteLine("<tr><th>Severity</th><th>Message</th><th>File</th><th>Line</th><th>Time</th></tr>");
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

            _writer.WriteLine($"<tr><td>{reportItem.Severity}</td><td>{Escape(reportItem.Message)}</td><td>{Escape(reportItem.File)}</td><td>{reportItem.Line}</td><td>{reportItem.DateTime}</td></tr>");
        }

        public void Dispose()
        {
            _writer.WriteLine("</table>");
            _writer.WriteLine("</body>");
            _writer.WriteLine("</html>");
            _writer.Dispose();
        }

        private MessageSeverity GetSeverity(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Diagnostic:
                    return MessageSeverity.Diagnostic;
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

        public string Escape(string html, bool encode = false)
        {
            return html == null ? null : ReplaceRegex(html, encode ? EscapeWithEncode : EscapeWithoutEncode, "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        public string ReplaceRegex(string input, Regex pattern, string replacement)
        {
            return pattern.Replace(input, replacement);
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
            Verbose,
            Diagnostic
        }
    }
}
