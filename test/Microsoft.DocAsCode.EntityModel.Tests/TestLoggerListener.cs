// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    public class TestLoggerListener : ILoggerListener
    {
        public string Phase { get; }

        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public LogLevel LogLevelThreshold { get; set; }

        public TestLoggerListener(string phase)
        {
            Phase = phase;
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        public void WriteLine(ILogItem item)
        {
            if (item.Phase == Phase)
            {
                Items.Add(item);
            }
        }
    }
}
