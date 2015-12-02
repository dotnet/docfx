// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
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
            _originPhaseName = CallContext.LogicalGetData(nameof(LoggerPhaseScope)) as string;
            if (_originPhaseName == null)
            {
                CallContext.LogicalSetData(nameof(LoggerPhaseScope), phaseName);
            }
            else
            {
                CallContext.LogicalSetData(nameof(LoggerPhaseScope), _originPhaseName + "." + phaseName);
            }
        }

        public void Dispose()
        {
            CallContext.LogicalSetData(nameof(LoggerPhaseScope), _originPhaseName);
        }

        internal static string GetPhaseName()
        {
            return CallContext.LogicalGetData(nameof(LoggerPhaseScope)) as string;
        }
    }
}
