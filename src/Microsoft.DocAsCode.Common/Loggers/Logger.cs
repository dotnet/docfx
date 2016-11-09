// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Immutable;

    public static class Logger
    {
        private static readonly object _sync = new object();
        private static ImmutableList<ILoggerListener> _listeners = ImmutableList<ILoggerListener>.Empty;

        public volatile static LogLevel LogLevelThreshold = LogLevel.Info;

        public static void RegisterListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (_sync)
            {
                _listeners = _listeners.Add(listener);
            }
        }

        public static ILoggerListener FindListener(Predicate<ILoggerListener> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return _listeners.Find(predicate);
        }

        public static void UnregisterListener(ILoggerListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock(_sync)
            {
                listener.Dispose();
                _listeners = _listeners.Remove(listener);
            }
        }

        public static void UnregisterAllListeners()
        {
            foreach (var i in _listeners)
            {
                i.Dispose();
            }

            _listeners = ImmutableList<ILoggerListener>.Empty;
        }

        public static void Log(ILogItem result)
        {
            if (result.LogLevel < LogLevelThreshold)
            {
                return;
            }

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
#if NetCore
                File = file,
#else
                File = file ?? LoggerFileScope.GetFileName(),
#endif
                Line = line,
                LogLevel = level,
                Message = message,
#if NetCore
                Phase = phase,
#else
                Phase = phase ?? LoggerPhaseScope.GetPhaseName(),
#endif
            });
        }

        public static void LogDiagnostic(string message, string phase = null, string file = null, string line = null)
        {
            Log(LogLevel.Diagnostic, message, phase, file, line);
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
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
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
        }
    }
}
