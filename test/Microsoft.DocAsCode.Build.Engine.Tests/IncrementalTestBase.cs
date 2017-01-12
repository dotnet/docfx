// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    public class IncrementalTestBase : TestBase
    {
        protected LogListener Listener { get; private set; }

        protected void IncrementalActions(string phaseName, params Action[] actions)
        {
            try
            {
                Init(phaseName);
                foreach (var action in actions)
                {
                    action();
                    ClearListener();
                }
            }
            finally
            {
                CleanUp();
            }
        }

        protected void Init(string phaseName)
        {
            Listener = new LogListener(phaseName) { LogLevelThreshold = LogLevel.Warning };
            Logger.RegisterListener(Listener);
        }

        protected void CleanUp()
        {
            Logger.UnregisterListener(Listener);
            Listener = null;
        }

        protected void ClearListener()
        {
            Listener?.Items.Clear();
        }

        protected List<string> GetLogMessages(params string[] phasePrefixes)
        {
            return (from i in Listener.Items
                    from p in phasePrefixes
                    where i.Phase.StartsWith(p)
                    orderby i.Message
                    select i.Message).ToList();
        }

        protected class LogListener : ILoggerListener
        {
            public string Phase { get; }

            public List<ILogItem> Items { get; } = new List<ILogItem>();

            public LogLevel LogLevelThreshold { get; set; }

            public LogListener(string phase)
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
}
