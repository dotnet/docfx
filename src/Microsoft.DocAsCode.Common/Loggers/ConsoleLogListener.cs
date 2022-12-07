// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;

    public sealed class ConsoleLogListener : ILoggerListener
    {
        private const LogLevel LogLevelThreshold = LogLevel.Verbose;

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var level = item.LogLevel;
            var message = item.Message;
            var phase = item.Phase;
            var file = item.File;
            var line = item.Line;
            if (level < LogLevelThreshold) return;
            var now = DateTime.UtcNow.ToString("yy-MM-dd hh:mm:ss.fff");

            var formatter = $"[{now}]{level}:";
            if (!string.IsNullOrEmpty(phase))
            {
                formatter += $"[{phase}]";
            }
            if (!string.IsNullOrEmpty(file))
            {
                string lineInfo = string.Empty;
                if (!string.IsNullOrEmpty(line))
                {
                    lineInfo = $"#L{line}";
                }
                formatter += $"({file.Replace('\\', '/')}{lineInfo})";
            }

            formatter += message;

            var consoleColor = GetConsoleColor(level);
            ConsoleUtility.WriteLine(formatter, consoleColor);
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    return ConsoleColor.Gray;
                case LogLevel.Info:
                    return ConsoleColor.White;
                case LogLevel.Suggestion:
                    return ConsoleColor.Blue;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }
    }
}