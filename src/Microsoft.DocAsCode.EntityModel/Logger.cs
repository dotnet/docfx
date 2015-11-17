// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;

    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
    }

    public interface ILogItem
    {
        LogLevel LogLevel { get; }
        string Message { get; }
        string Phase { get; }
        string File { get; }
        string Line { get; }
    }

    public static class Logger
    {
        private static ImmutableList<ILoggerListener> _listeners = ImmutableList<ILoggerListener>.Empty;
        private static readonly object _sync = new object();

        public volatile static LogLevel LogLevelThreshold = LogLevel.Info;

        public static void RegisterListener(ILoggerListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            _listeners = _listeners.Add(listener);
        }

        public static void UnregisterListener(ILoggerListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            listener.Dispose();
            _listeners = _listeners.Remove(listener);
        }

        public static void UnregisterAllListeners()
        {
            foreach(var i in _listeners)
            {
                i.Dispose();
            }

            _listeners = ImmutableList<ILoggerListener>.Empty;
        }

        public static void Log(ILogItem result)
        {
            if (result.LogLevel < LogLevelThreshold) return;
            lock (_sync)
            {
                foreach (var listener in _listeners)
                {
                    listener.WriteLine(result);
                }
            }
        }

        public static void Log(LogLevel level, string message, string phase = null, string file = null, string line = null)
        {
            Log(new LogItem
            {
                File = file ?? LoggerFileScope.GetFileName(),
                Line = line,
                LogLevel = level,
                Message = message,
                Phase = phase ?? LoggerPhaseScope.GetPhaseName(),
            });
        }

        public static void LogVerbose(string message, string phase = null, string file = null, string line = null)
        {
            Log(LogLevel.Verbose, message, phase, file, line);
        }

        public static void LogInfo(string message, string phase = null, string file = null, string line = null)
        {
            Log(LogLevel.Info, message, phase, file, line);
        }

        public static void LogWarning(string message, string phase = null, string file = null, string line = null)
        {
            Log(LogLevel.Warning, message, phase, file, line);
        }

        public static void LogError(string message, string phase = null, string file = null, string line = null)
        {
            Log(LogLevel.Error, message, phase, file, line);
        }

        public static void Log(object result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Log(LogLevel.Info, result.ToString());
        }

        public static void Flush()
        {
            lock (_sync)
            {
                foreach (var listener in _listeners)
                {
                    listener.Flush();
                }
            }
        }

        private class LogItem : ILogItem
        {
            public string File { get; set; }

            public string Line { get; set; }

            public LogLevel LogLevel { get; set; }

            public string Message { get; set; }

            public string Phase { get; set; }
        }
    }

    public interface ILoggerListener : IDisposable
    {
        LogLevel LogLevelThreshold { get; set; }
        void WriteLine(ILogItem item);
        void Flush();
    }

    public sealed class ConsoleLogListener : ILoggerListener
    {
        public LogLevel LogLevelThreshold { get; set; }

        public void WriteLine(ILogItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var level = item.LogLevel;
            var message = item.Message;
            var phase = item.Phase;
            var file = item.File;
            var line = item.Line;
            if (level < LogLevelThreshold) return;
            var formatter = level + ": " + message;
            if (!string.IsNullOrEmpty(phase)) formatter += " in phase " + phase;
            if (!string.IsNullOrEmpty(file))
            {
                formatter += " in file " + file;
                if (!string.IsNullOrEmpty(line)) formatter += " line " + line;
            }

            var foregroundColor = Console.ForegroundColor;
            try
            {
                ChangeConsoleColor(level);
                Console.WriteLine(formatter);
            }
            finally
            {
                Console.ForegroundColor = foregroundColor;
            }
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        private void ChangeConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }
    }

    public sealed class ReportLogListener : ILoggerListener
    {
        private TextWriterTraceListener _listener;

        public LogLevel LogLevelThreshold { get; set; }

        public ReportLogListener(string reportPath)
        {
            var dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _listener = new TextWriterTraceListener(reportPath);
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
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

            _listener.WriteLine(JsonUtility.Serialize(reportItem));
        }

        public void Dispose()
        {
            _listener.Dispose();
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
            _listener.Flush();
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
