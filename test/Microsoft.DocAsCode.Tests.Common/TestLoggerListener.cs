// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class TestLoggerListener : ILoggerListener
    {
        public string Phase { get; }

        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public LogLevel LogLevelThreshold { get; set; }

        public Func<ILogItem, bool> PhaseMatcher { get; set; }

        public TestLoggerListener(string phase = null,
            LogLevel logLevelThreshold = LogLevel.Warning,
            Func<ILogItem, bool> phaseMatcher = null)
        {
            Phase = phase;
            LogLevelThreshold = logLevelThreshold;
            if (phaseMatcher == null)
            {
                // Set default phase matcher to start with current phase
                PhaseMatcher = iLogItem => iLogItem?.Phase != null && iLogItem.Phase.StartsWith(Phase);
            }
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null || item.LogLevel < LogLevelThreshold)
            {
                return;
            }
            if (Phase == null ||
                Phase.Equals(item.Phase, StringComparison.OrdinalIgnoreCase) ||
                PhaseMatcher(item))
            {
                Items.Add(item);
            }
        }

        public ILogItem TakeAndRemove()
        {
            if (Items.Count == 0)
            {
                return null;
            }
            var result = Items[0];
            Items.RemoveAt(0);
            return result;
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }
    }
}
