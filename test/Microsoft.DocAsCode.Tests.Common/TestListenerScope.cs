// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class TestListenerScope : IDisposable
    {
        private readonly TestLoggerListener _listener;
        private readonly LoggerPhaseScope _scope;

        public TestListenerScope(string phaseName)
        {
            _listener = TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter(phaseName);
            Logger.RegisterListener(_listener);
            _scope = new LoggerPhaseScope(phaseName);
        }

        public void Dispose()
        {
            _scope.Dispose();
            Logger.UnregisterListener(_listener);
            _listener.Dispose();
        }

        public List<ILogItem> Items => _listener.Items;
    }
}
