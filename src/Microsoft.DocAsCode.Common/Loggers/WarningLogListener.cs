// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Concurrent;

    public sealed class WarningLogListener : ILoggerListener
    {
        private readonly ConcurrentBag<WarningLog> _logs;

        private const LogLevel LogLevelThreshold = LogLevel.Warning;

        public WarningLogListener(ConcurrentBag<WarningLog> logs)
        {
            _logs = logs ?? new ConcurrentBag<WarningLog>();
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
            if (item.LogLevel != LogLevelThreshold) return;
            _logs.Add(new WarningLog { Message = item.Message, File = item.File, Line = item.Line, Phase = item.Phase });
        }
    }

    public class WarningLog
    {
        public string Message { get; set; }
        public string Phase { get; set; }
        public string File { get; set; }
        public string Line { get; set; }
        public LogLevel Level { get { return LogLevel.Warning; } }
    }
}
