using ECMA2Yaml.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace ECMA2Yaml
{
    public class OPSLogger
    {
        public static string PathTrimPrefix = "";
        public static string FallbackPathTrimPrefix = "";
        public static Action<LogItem> WriteLogCallback = null;
        public static bool ErrorLogged { get; private set; }

        private static ConcurrentBag<LogItem> logBag = new ConcurrentBag<LogItem>();

        public static void LogUserError(LogCode code, string file, params object[] msgArgs)
        {
            var message = string.Format(code.MessageTemplate, msgArgs);
            file = TranslateFilePath(file);
            WriteLog(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Error, code.ToString(), LogItemType.User));
        }

        public static void LogUserWarning(LogCode code, string file, params object[] msgArgs)
        {
            var message = string.Format(code.MessageTemplate, msgArgs);
            file = TranslateFilePath(file);
            WriteLog(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Warning, code.ToString(), LogItemType.User));
        }

        public static void LogUserSuggestion(LogCode code, string file, params object[] msgArgs)
        {
            var message = string.Format(code.MessageTemplate, msgArgs);
            file = TranslateFilePath(file);
            WriteLog(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Suggestion, code.ToString(), LogItemType.User));
        }

        public static void LogUserInfo(string message, string file = null)
        {
            file = TranslateFilePath(file);
            WriteLog(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Info, LogCode.ECMA2Yaml_Info.ToString(), LogItemType.User));
        }

        public static void LogSystemError(LogCode code, string file, params object[] msgArgs)
        {
            var message = string.Format(code.MessageTemplate, msgArgs);
            file = TranslateFilePath(file);
            WriteLog(new LogItem(message, "ECMA2Yaml", file, MessageSeverity.Error, code.ToString(), LogItemType.System));
        }

        public static void Flush(string filePath)
        {
            if (logBag.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var log in logBag.ToArray())
                {
                    var logStr = JsonConvert.SerializeObject(log);
                    sb.AppendLine(logStr);
                    if (log.MessageSeverity == MessageSeverity.Error)
                    {
                        Console.WriteLine(logStr);
                        Environment.ExitCode = -1;
                    }
                }
                File.AppendAllText(filePath, sb.ToString());
            }
        }

        private static string TranslateFilePath(string file)
        {
            if (string.IsNullOrEmpty(PathTrimPrefix) || string.IsNullOrEmpty(file))
            {
                return file;
            }
            file = file.NormalizePath();
            if (!string.IsNullOrEmpty(PathTrimPrefix))
            {
                if (file.StartsWith(PathTrimPrefix))
                {
                    file = file.Replace(PathTrimPrefix, "");
                }
                else
                {
                    file = FileAbstractLayer.RelativePath(file, PathTrimPrefix, false);
                }
            }
            if (!string.IsNullOrEmpty(FallbackPathTrimPrefix))
            {
                file = file.Replace(FallbackPathTrimPrefix, "_repo.en-us" + Path.DirectorySeparatorChar);
            }
            return file;
        }

        private static void WriteLog(LogItem logItem)
        {
            if (logItem.MessageSeverity == MessageSeverity.Error)
            {
                ErrorLogged = true;
            }
            if (WriteLogCallback != null)
            {
                WriteLogCallback(logItem);
            }
            else
            {
                logBag.Add(logItem);
            }
        }
    }
}
