// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Tests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal class TestLoggerListener : ILoggerListener
    {
        private readonly Func<ILogItem, bool> _filter;
        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public TestLoggerListener(Func<ILogItem, bool> filter)
        {
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_filter(item))
            {
                Items.Add(item);
            }
        }

        public static TestLoggerListener CreateLoggerListenerWithPhaseEqualFilter(string phase, LogLevel logLevel = LogLevel.Warning)
        {
            return new TestLoggerListener(iLogItem =>
            {
                if (iLogItem.LogLevel < logLevel)
                {
                    return false;
                }

                if (phase == null || (iLogItem?.Phase != null && iLogItem.Phase == phase))
                {
                    return true;
                }

                return false;
            });
        }
    }
}