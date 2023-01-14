// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;
    using System.Text;
    using Microsoft.DocAsCode.Plugins;

    public sealed class ConsoleLogListener : ILoggerListener
    {
        private const LogLevel LogLevelThreshold = LogLevel.Verbose;

        public void WriteLine(ILogItem item)
        {
            var level = item.LogLevel;
            if (level < LogLevelThreshold)
                return;

            var consoleColor = GetConsoleColor(level);

            var message = new StringBuilder();
            if (!string.IsNullOrEmpty(item.File))
            {
                message.Append($"{Path.GetFullPath(Path.Combine(EnvironmentContext.BaseDirectory, item.File))}");
                if (!string.IsNullOrEmpty(item.Line))
                    message.Append($"({item.Line},1)");
                if (!string.IsNullOrEmpty(item.Code))
                    message.Append($": {item.LogLevel.ToString().ToLowerInvariant()} {item.Code}");
                message.Append(": ");
            }

            message.Append(item.Message);

            ConsoleUtility.WriteLine(message.ToString(), consoleColor);
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