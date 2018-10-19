// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public sealed class LoggerPhaseScope : IDisposable
    {
        private readonly string _originPhaseName;
        private readonly PerformanceScope _performanceScope;
        private readonly AmbientContext? _ac;
        public LoggerPhaseScope(string phaseName)
            : this(phaseName, null) { }

        public LoggerPhaseScope(string phaseName, LogLevel perfLogLevel)
            : this(phaseName, (LogLevel?)perfLogLevel) { }

        private LoggerPhaseScope(string phaseName, LogLevel? perfLogLevel)
        {
            if (string.IsNullOrWhiteSpace(phaseName))
            {
                throw new ArgumentException("Phase name cannot be null or white space.", nameof(phaseName));
            }

            _ac = AmbientContext.GetOrCreateAmbientContext().CreateBranch();

            _originPhaseName = GetPhaseName();
            phaseName = _originPhaseName == null ? phaseName : _originPhaseName + "." + phaseName;
            SetPhaseName(phaseName);
            if (perfLogLevel != null)
            {
                _performanceScope = new PerformanceScope("Scope:" + phaseName, perfLogLevel.Value);
            }
        }

        private LoggerPhaseScope(CapturedLoggerPhaseScope captured, LogLevel? perfLogLevel)
        {
            _originPhaseName = GetPhaseName();
            var context = captured.CurrentAmbientContext;
            if (context.HasValue)
            {
                _ac = new AmbientContext(context.Value);
            }
            SetPhaseName(captured.PhaseName);
            if (perfLogLevel != null)
            {
                _performanceScope = new PerformanceScope("Scope:" + captured.PhaseName, perfLogLevel.Value);
            }
        }

        public static T WithScope<T>(string phaseName, Func<T> func)
        {
            using (new LoggerPhaseScope(phaseName))
            {
                return func();
            }
        }

        public static T WithScope<T>(string phaseName, LogLevel perfLogLevel, Func<T> func)
        {
            using (new LoggerPhaseScope(phaseName, perfLogLevel))
            {
                return func();
            }
        }

        public void Dispose()
        {
            _performanceScope?.Dispose();
            SetPhaseName(_originPhaseName);
            _ac?.Dispose();
        }

        internal static string GetPhaseName()
        {
            return LogicalCallContext.GetData(nameof(LoggerPhaseScope)) as string;
        }

        private void SetPhaseName(string phaseName)
        {
            LogicalCallContext.SetData(nameof(LoggerPhaseScope), phaseName);
        }

        public static object Capture()
        {
            return new CapturedLoggerPhaseScope();
        }

        public static LoggerPhaseScope Restore(object captured) =>
            Restore(captured, null);

        public static LoggerPhaseScope Restore(object captured, LogLevel perfLogLevel) =>
            Restore(captured, (LogLevel?)perfLogLevel);

        private static LoggerPhaseScope Restore(object captured, LogLevel? perfLogLevel)
        {
            if (!(captured is CapturedLoggerPhaseScope capturedScope))
            {
                return null;
            }
            return new LoggerPhaseScope(capturedScope, perfLogLevel);
        }

        private sealed class CapturedLoggerPhaseScope
        {
            public CapturedLoggerPhaseScope()
            {
                PhaseName = GetPhaseName();
                CurrentAmbientContext = AmbientContext.CurrentContext;
            }

            public string PhaseName { get; }
            public AmbientContext? CurrentAmbientContext { get; }

        }
    }
}
