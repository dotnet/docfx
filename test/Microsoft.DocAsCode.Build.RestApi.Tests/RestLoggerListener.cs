// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    internal sealed class RestLoggerListener : ILoggerListener
    {
        public string Phase { get; }

        public LogLevel LogLevelThreshold { get; set; }

        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public RestLoggerListener(string phase, LogLevel logLevelThreshold = LogLevel.Warning)
        {
            Phase = phase;
            LogLevelThreshold = logLevelThreshold;
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void WriteLine(ILogItem item)
        {
            if (item.LogLevel < LogLevelThreshold)
            {
                return;
            }
            if (item.Phase == Phase)
            {
                Items.Add(item);
            }
            else if (item.Phase != null && item.Phase.StartsWith(Phase))
            {
                Items.Add(item);
            }
        }
    }
}
