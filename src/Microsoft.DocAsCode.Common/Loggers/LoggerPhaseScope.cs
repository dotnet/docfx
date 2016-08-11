// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Runtime.Remoting.Messaging;

    public sealed class LoggerPhaseScope : IDisposable
    {
        private readonly string _originPhaseName;
        private readonly PerformanceScope _performanceScope;

        public LoggerPhaseScope(string phaseName)
            : this(phaseName, false)
        {
        }

        public LoggerPhaseScope(string phaseName, bool enablePerformanceScope)
        {
            if (string.IsNullOrWhiteSpace(phaseName))
            {
                throw new ArgumentException("Phase name cannot be null or white space.", nameof(phaseName));
            }
            _originPhaseName = GetPhaseName();
            phaseName = _originPhaseName == null ? phaseName : _originPhaseName + "." + phaseName;
            SetPhaseName(phaseName);
            if (enablePerformanceScope)
            {
                _performanceScope = new PerformanceScope("Scope:" + phaseName, LogLevel.Diagnostic);
            }
        }

        private LoggerPhaseScope(CapturedLoggerPhaseScope captured, bool enablePerformanceScope)
        {
            _originPhaseName = GetPhaseName();
            SetPhaseName(captured.PhaseName);
            if (enablePerformanceScope)
            {
                _performanceScope = new PerformanceScope("Scope:" + captured.PhaseName, LogLevel.Diagnostic);
            }
        }

        public void Dispose()
        {
            _performanceScope?.Dispose();
            SetPhaseName(_originPhaseName);
        }

        internal static string GetPhaseName()
        {
            return CallContext.LogicalGetData(nameof(LoggerPhaseScope)) as string;
        }

        private void SetPhaseName(string phaseName)
        {
            CallContext.LogicalSetData(nameof(LoggerPhaseScope), phaseName);
        }

        public static object Capture()
        {
            return new CapturedLoggerPhaseScope();
        }

        public static LoggerPhaseScope Restore(object captured)
            => Restore(captured, false);

        public static LoggerPhaseScope Restore(object captured, bool enablePerformanceScope)
        {
            var capturedScope = captured as CapturedLoggerPhaseScope;
            if (capturedScope == null)
            {
                return null;
            }
            return new LoggerPhaseScope(capturedScope, enablePerformanceScope);
        }

        private sealed class CapturedLoggerPhaseScope
        {
            public CapturedLoggerPhaseScope()
            {
                PhaseName = GetPhaseName();
            }

            public string PhaseName { get; }
        }
    }
}
#endif