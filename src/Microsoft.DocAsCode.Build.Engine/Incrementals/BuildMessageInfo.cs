// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;


    public sealed class BuildMessageInfo
    {
        private Listener _listener;
        private readonly OSPlatformSensitiveDictionary<List<LogItem>> _logs;

        public BuildMessageInfo()
        {
            _logs = new OSPlatformSensitiveDictionary<List<LogItem>>();
        }

        private BuildMessageInfo(IDictionary<string, List<LogItem>> logs)
        {
            if (logs == null)
            {
                throw new ArgumentNullException(nameof(logs));
            }
            _logs = new OSPlatformSensitiveDictionary<List<LogItem>>(logs);
        }

        /// <summary>
        /// Get messages logged for file
        /// </summary>
        /// <param name="file">file path from working directory</param>
        /// <returns>logged messages</returns>
        public IEnumerable<ILogItem> GetMessages(string file)
        {
            if (_logs.TryGetValue(file, out List<LogItem> messages))
            {
                return messages;
            }
            return Enumerable.Empty<ILogItem>();
        }

        public ILoggerListener GetListener()
        {
            if (_listener == null)
            {
                _listener = new Listener(this);
            }
            return _listener;
        }

        /// <summary>
        /// relay messages for file
        /// </summary>
        /// <param name="file">file path from working directory</param>
        public void Replay(string file)
        {
            foreach (var item in GetMessages(file))
            {
                Logger.Log(item);
            }
        }

        private void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (item.File == null)
            {
                return;
            }
            string fileFromWorkingDir = StringExtension.BackSlashToForwardSlash(item.File);
            if (!PathUtility.IsRelativePath(item.File))
            {
                fileFromWorkingDir = PathUtility.MakeRelativePath(EnvironmentContext.BaseDirectory, item.File);
            }
            if (!_logs.TryGetValue(fileFromWorkingDir, out List<LogItem> logsPerFile))
            {
                logsPerFile = _logs[fileFromWorkingDir] = new List<LogItem>();
            }
            logsPerFile.Add(new LogItem
            {
                File = StringExtension.BackSlashToForwardSlash(item.File),
                Line = item.Line,
                LogLevel = item.LogLevel,
                Message = item.Message,
                Phase = item.Phase,
                Code = item.Code
            });
        }

        public static BuildMessageInfo Load(TextReader reader)
        {
            var logs = JsonUtility.Deserialize<IDictionary<string, List<LogItem>>>(reader);
            return new BuildMessageInfo(logs);
        }

        public void Save(TextWriter writer)
        {
            JsonUtility.Serialize(writer, _logs);
        }

        private sealed class Listener : ILoggerListener
        {
            private readonly BuildMessageInfo _bmi;

            public Listener(BuildMessageInfo bmi)
            {
                _bmi = bmi ?? throw new ArgumentNullException(nameof(bmi));
            }

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void WriteLine(ILogItem item)
            {
                if (item.LogLevel >= LogLevel.Suggestion)
                {
                    _bmi.WriteLine(item);
                }
            }
        }

        [Serializable]
        private sealed class LogItem : ILogItem
        {
            public string File { get; set; }

            public string Line { get; set; }

            public LogLevel LogLevel { get; set; }

            public string Message { get; set; }

            public string Phase { get; set; }

            public string Code { get; set; }

            public string CorrelationId { get; set; }
        }
    }
}
