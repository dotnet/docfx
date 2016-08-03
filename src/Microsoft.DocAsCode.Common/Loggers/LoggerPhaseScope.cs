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

        public LoggerPhaseScope(string phaseName)
        {
            if (string.IsNullOrWhiteSpace(phaseName))
            {
                throw new ArgumentException("Phase name cannot be null or white space.", nameof(phaseName));
            }
            _originPhaseName = GetPhaseName();
            SetPhaseName(_originPhaseName == null ? phaseName : _originPhaseName + "." + phaseName);
        }

        private LoggerPhaseScope(CapturedLoggerPhaseScope captured)
        {
            _originPhaseName = GetPhaseName();
            SetPhaseName(captured.PhaseName);
        }

        public void Dispose()
        {
            CallContext.LogicalSetData(nameof(LoggerPhaseScope), _originPhaseName);
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
            return new CapturedLoggerPhaseScope { };
        }

        public static LoggerPhaseScope Restore(object captured)
        {
            var capturedScope = captured as CapturedLoggerPhaseScope;
            if (capturedScope == null)
            {
                return null;
            }
            return new LoggerPhaseScope(capturedScope);
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