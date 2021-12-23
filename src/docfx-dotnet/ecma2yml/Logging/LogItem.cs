using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;

namespace ECMA2Yaml
{
    public enum MessageSeverity
    {
        Error,
        Warning,
        Info,
        Verbose,
        Diagnostic,
        Suggestion
    }

    public enum LogItemType
    {
        Unspecified,
        System,
        User,
    }

    public class LogItem
    {
        /// <summary>
        /// Message to log
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Project class that throw the exception
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Processing file
        /// </summary>
        [JsonProperty("file")]
        public string File { get; set; }

        /// <summary>
        /// Line number in file
        /// </summary>
        [JsonProperty("line")]
        public int? Line { get; set; }

        /// <summary>
        /// Message Severity
        /// TODO: message_serverity will be retired in the future, please use the message_severity_string property instead
        /// </summary>
        [JsonProperty("message_severity")]
        public MessageSeverity MessageSeverity { get; set; }

        /// <summary>
        /// Message severity string, could be "Error", "Warning", "Suggestion", "Info", "Verbose" or "Diagnostic"
        /// </summary>
        [JsonProperty("message_severity_string")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageSeverity MessageSeverityString { get; set; }

        /// <summary>
        /// Log code of the item
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Log time
        /// </summary>
        [JsonProperty("date_time")]
        public DateTime DateTime { get; set; }

        /// <summary>
        /// The type of the log item, could be user or system
        /// TODO: log_item_type will be retired in the future, please use the log_item_type_string property instead
        /// </summary>
        [JsonProperty("log_item_type")]
        public LogItemType LogItemType { get; set; }

        /// <summary>
        /// The string of the log item type, could be "Unspecified", "System" or "User"
        /// </summary>
        [JsonProperty("log_item_type_string")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogItemType LogItemTypeString { get; set; }

        /// <summary>
        /// The category of the log item
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        private LogItem()
        {
        }

        public LogItem(
            string message,
            string source,
            string file,
            MessageSeverity messageSeverity,
            string code,
            LogItemType logItemType)
            : this(message, source, file, null, DateTime.UtcNow, messageSeverity, code, logItemType)
        {
        }

        [JsonConstructor]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LogItem(
            string message,
            string source,
            string file,
            int? line,
            DateTime dateTime,
            MessageSeverity messageSeverity,
            string code,
            LogItemType logItemType)
        {
            Message = message;
            Source = source;
            File = file;
            Line = line;
            DateTime = dateTime;
            MessageSeverity = messageSeverity;
            MessageSeverityString = messageSeverity;
            Code = code;
            LogItemType = logItemType;
            LogItemTypeString = logItemType;
            Category = "ECMA2Yaml";
        }
    }
}
