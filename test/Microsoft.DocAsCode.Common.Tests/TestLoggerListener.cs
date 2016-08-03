﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class TestLoggerListener : ILoggerListener
    {
        public string Phase { get; }

        public List<ILogItem> Items { get; } = new List<ILogItem>();

        public LogLevel LogLevelThreshold { get; set; }

        public TestLoggerListener(string phase = null)
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
            if (Phase == null || item.Phase == Phase)
            {
                Items.Add(item);
            }
        }
    }
}
