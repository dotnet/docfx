// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests.Common
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class TestLoggerListener : ILoggerListener
    {
        public string Phase { get; }

        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public LogLevel LogLevelThreshold { get; set; }

        public bool EnablePhaseEndWith { get; set; }

        public TestLoggerListener()
        {
        }

        public TestLoggerListener(string phase, LogLevel logLevelThreshold = LogLevel.Warning, bool enablePhaseEndWith = false)
        {
            Phase = phase;
            LogLevelThreshold = logLevelThreshold;
            EnablePhaseEndWith = enablePhaseEndWith;
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null || item.LogLevel < LogLevelThreshold)
            {
                return;
            }
            if (Phase == null ||
                item.Phase == Phase ||
                !EnablePhaseEndWith && item.Phase.StartsWith(Phase) ||
                EnablePhaseEndWith && item.Phase.EndsWith(Phase))
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
