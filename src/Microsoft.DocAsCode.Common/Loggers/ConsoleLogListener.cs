// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NetCore
namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

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

            var foregroundColor = Console.ForegroundColor;
            try
            {
                ChangeConsoleColor(level);
                Console.WriteLine(formatter);
            }
            finally
            {
                Console.ForegroundColor = foregroundColor;
            }
        }

        public void Dispose()
        {
        }

        public void Flush()
        {
        }

        private void ChangeConsoleColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Suggestion:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }
    }
}
#endif