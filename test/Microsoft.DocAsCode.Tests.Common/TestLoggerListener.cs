// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests.Common
{
    using System;
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;

    public class TestLoggerListener : ILoggerListener
    {
        public List<ILogItem> Items { get; } = new List<ILogItem>();

        private Func<ILogItem, bool> _matcher;

        private TestLoggerListener(Func<ILogItem, bool> matcher)
        {
            _matcher = matcher;
        }

        #region ILoggerListener

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (_matcher(item))
            {
                Items.Add(item);
            }
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }

        #endregion

        #region Creators

        public static TestLoggerListener CreateLoggerListener(Func<ILogItem, bool> matcher)
        {
            return new TestLoggerListener(matcher);
        }

        public static TestLoggerListener CreateLoggerListenerWithPhaseStartMatcher(string phase, LogLevel logLevel = LogLevel.Warning)
        {
            return new TestLoggerListener(iLogItem =>
            {
                if (iLogItem.LogLevel < logLevel)
                {
                    return false;
                }
                if (phase == null ||
                   (iLogItem?.Phase != null && iLogItem.Phase.StartsWith(phase, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                return false;
            });
        }

        public static TestLoggerListener CreateLoggerListenerWithPhaseEndMatcher(string phase, LogLevel logLevel = LogLevel.Warning)
        {
            return new TestLoggerListener(iLogItem =>
            {
                if (iLogItem.LogLevel < logLevel)
                {
                    return false;
                }
                if (phase == null ||
                   (iLogItem?.Phase != null && iLogItem.Phase.EndsWith(phase, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                return false;
            });
        }

        public static TestLoggerListener CreateLoggerListenerWithPhaseEqualMatcher(string phase, LogLevel logLevel = LogLevel.Warning)
        {
            return new TestLoggerListener(iLogItem =>
            {
                if (iLogItem.LogLevel < logLevel)
                {
                    return false;
                }
                if (phase == null ||
                   (iLogItem?.Phase != null && iLogItem.Phase.Equals(phase, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
                return false;
            });
        }

        #endregion

        #region Helpers 

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

        #endregion
    }
}
