// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Linq;

    /// <summary>
    /// Replay aggregated log on flushing
    /// </summary>
    public class AggregatedLogListener : ILoggerListener
    {
        private readonly LogLevel _threshold;
        private readonly int[] _count = new int[Enum.GetNames(typeof(LogLevel)).Length];

        public AggregatedLogListener(LogLevel threshold = LogLevel.Warning)
        {
            _threshold = threshold;
        }

        public void Flush()
        {
            var logLevel = (LogLevel)Enumerable.Range(0, _count.Length).LastOrDefault(i => _count[i] > 0);
            var status = GetBuildStatusFromLogLevel(logLevel);
            var summary = Environment.NewLine + Environment.NewLine;
            switch (status)
            {
                case BuildStatus.Failed:
                    summary += "Build failed.";
                    break;
                case BuildStatus.SucceededWithWarning:
                    summary += "Build succeeded with warning.";
                    break;
                case BuildStatus.Succeeded:
                    summary += "Build succeeded.";
                    break;
                default:
                    break;
            }

            WriteToConsole(summary, status);

            for (var i = 0; i < _count.Length; i++)
            {
                var level = (LogLevel)i;
                if (level >= _threshold && level <= LogLevel.Error)
                {
                    WriteToConsole($"    {_count[i]} {level}(s)", status);
                }
            }
        }

        public void Dispose()
        {
        }

        public void WriteLine(ILogItem item)
        {
            _count[(int)item.LogLevel]++;
        }

        private static void WriteToConsole(string message, BuildStatus status)
        {
            switch (status)
            {
                case BuildStatus.Failed:
                    WriteToConsole(message, ConsoleColor.Red);
                    break;
                case BuildStatus.SucceededWithWarning:
                    WriteToConsole(message, ConsoleColor.Yellow);
                    break;
                case BuildStatus.Succeeded:
                    WriteToConsole(message, ConsoleColor.Green);
                    break;
                default:
                    break;
            }
        }

        private static void WriteToConsole(string message, ConsoleColor color = ConsoleColor.White)
        {
            ConsoleUtility.WriteLine(message, color);
        }

        private static BuildStatus GetBuildStatusFromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Error:
                    return BuildStatus.Failed;
                case LogLevel.Warning:
                    return BuildStatus.SucceededWithWarning;
                default:
                    return BuildStatus.Succeeded;
            }
        }

        private enum BuildStatus
        {
            Failed,
            SucceededWithWarning,
            Succeeded
        }
    }
}
