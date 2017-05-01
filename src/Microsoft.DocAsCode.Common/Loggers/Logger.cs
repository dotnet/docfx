// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public static class Logger
    {
        public static bool HasError { get; private set; }

        private static readonly object _sync = new object();
        private static CompositeLogListener _syncListener = new CompositeLogListener();
        private static AsyncLogListener _asyncListener = new AsyncLogListener();
        public volatile static LogLevel LogLevelThreshold = LogLevel.Info;

        public static void RegisterListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _syncListener.AddListener(listener);
        }

        public static ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _syncListener.FindListener(predicate);
        }

        public static void UnregisterListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _syncListener.RemoveListener(listener);
        }

        public static void RegisterAsyncListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _asyncListener.AddListener(listener);
        }

        public static ILoggerListener FindAsyncListener(Predicate<ILoggerListener> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _asyncListener.FindListener(predicate);
        }

        public static void UnregisterAsyncListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            _asyncListener.RemoveListener(listener);
        }

        public static void UnregisterAllListeners()
        {
            _syncListener.RemoveAllListeners();
            _asyncListener.RemoveAllListeners();
        }

        public static void Log(ILogItem item)
        {
            if (item.LogLevel < LogLevelThreshold)
            {
                return;
            }

            if (item.LogLevel == LogLevel.Error)
            {
                HasError = true;
            }

            _syncListener.WriteLine(item);
            _asyncListener.WriteLine(item);
        }

        public static void Log(LogLevel level, string message, string phase = null, string file = null, string line = null)
        {
            Log(level, message, phase, file, line, null);
        }

        public static void Log(LogLevel level, string message, string phase, string file, string line, string code)
        {
            Log(new LogItem
            {
#if NetCore
                File = file,
#else
                File = file ?? LoggerFileScope.GetFileName(),
#endif
                Line = line,
                LogLevel = level,
                Message = message,
                Code = code,
#if NetCore
                Phase = phase,
#else
                Phase = phase ?? LoggerPhaseScope.GetPhaseName(),
#endif
            });
        }

        public static void LogDiagnostic(string message, string phase = null, string file = null, string line = null)
        {
            LogDiagnostic(message, phase, file, line, null);
        }

        public static void LogDiagnostic(string message, string phase, string file, string line, string code)
        {
            Log(LogLevel.Diagnostic, message, phase, file, line, code);
        }

        public static void LogVerbose(string message, string phase = null, string file = null, string line = null)
        {
            LogVerbose(message, phase, file, line, null);
        }

        public static void LogVerbose(string message, string phase, string file, string line, string code)
        {
            Log(LogLevel.Verbose, message, phase, file, line, code);
        }

        public static void LogInfo(string message, string phase = null, string file = null, string line = null)
        {
            LogInfo(message, phase, file, line, null);
        }

        public static void LogInfo(string message, string phase, string file, string line, string code)
        {
            Log(LogLevel.Info, message, phase, file, line, code);
        }

        public static void LogWarning(string message, string phase = null, string file = null, string line = null)
        {
            LogWarning(message, phase, file, line, null);
        }

        public static void LogWarning(string message, string phase, string file, string line, string code)
        {
            Log(LogLevel.Warning, message, phase, file, line, code);
        }

        public static void LogError(string message, string phase = null, string file = null, string line = null)
        {
            LogError(message, phase, file, line, null);
        }

        public static void LogError(string message, string phase, string file, string line, string code)
        {
            Log(LogLevel.Error, message, phase, file, line, code);
        }

        public static void Log(object result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            Log(LogLevel.Info, result.ToString());
        }

        public static void Flush()
        {
            _syncListener.Flush();
            _asyncListener.Flush();
        }

#if !NetCore
        [Serializable]
#endif
        private class LogItem : ILogItem
        {
            public string File { get; set; }

            public string Line { get; set; }

            public LogLevel LogLevel { get; set; }

            public string Message { get; set; }

            public string Phase { get; set; }

            public string Code { get; set; }
        }
    }
}
