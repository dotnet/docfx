// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class Error
    {
        public static readonly IEqualityComparer<Error> Comparer = new EqualityComparer();

        public ErrorLevel Level { get; }

        public string Code { get; }

        public string Message { get; }

        public string? MsAuthor { get; }

        public string? PropertyPath { get; }

        public SourceInfo? Source { get; }

        public PathString? OriginalPath { get; }

        public bool PullRequestOnly { get; }

        public object?[] MessageArguments { get; }

        public Error(ErrorLevel level, string code, FormattableString message, SourceInfo? source = null, string? propertyPath = null)
        {
            Level = level;
            Code = code;
            Message = message.ToString();
            MessageArguments = message.GetArguments();
            Source = source;
            PropertyPath = propertyPath;
        }

        public Error(
            ErrorLevel level,
            string code,
            string message,
            object?[] messageArguments,
            SourceInfo? source,
            string? propertyPath,
            PathString? originalPath,
            bool pullRequestOnly,
            string? msAuthor)
        {
            Level = level;
            Code = code;
            Message = message;
            MessageArguments = messageArguments;
            Source = source;
            PropertyPath = propertyPath;
            OriginalPath = originalPath;
            PullRequestOnly = pullRequestOnly;
            MsAuthor = msAuthor;
        }

        public Error WithLevel(ErrorLevel level)
        {
            return level == Level ? this : new Error(level, Code, Message, MessageArguments, Source, PropertyPath, OriginalPath, PullRequestOnly, MsAuthor);
        }

        public Error WithOriginalPath(PathString? originalPath)
        {
            return originalPath == OriginalPath ?
                this : new Error(Level, Code, Message, MessageArguments, Source, PropertyPath, originalPath, PullRequestOnly, MsAuthor);
        }

        public Error WithSource(SourceInfo? source)
        {
            return new Error(Level, Code, Message, MessageArguments, source, PropertyPath, OriginalPath, PullRequestOnly, MsAuthor);
        }

        public Error WithMsAuthor(string? msAuthor)
        {
            return new Error(Level, Code, Message, MessageArguments, Source, PropertyPath, OriginalPath, PullRequestOnly, msAuthor);
        }

        public Error WithPropertyPath(string? propertyPath)
        {
            return new Error(Level, Code, Message, MessageArguments, Source, propertyPath, OriginalPath, PullRequestOnly, MsAuthor);
        }

        public override string ToString()
        {
            var file = OriginalPath ?? Source?.File?.Path;
            var source = OriginalPath is null ? Source : null;
            var line = source?.Line ?? 0;
            var end_line = source?.EndLine ?? 0;
            var column = source?.Column ?? 0;
            var end_column = source?.EndColumn ?? 0;
            return JsonUtility.Serialize(new ErrorItem
            {
                MessageSeverity = Level,
                Code = Code,
                Message = Message,
                File = file,
                Line = line,
                EndLine = end_line,
                Column = column,
                EndColumn = end_column,
                LogItemType = "user",
                PullRequestOnly = PullRequestOnly ? (bool?)true : null,
                PropertyPath = PropertyPath,
                DateTime = DateTime.UtcNow, // Leave data_time as the last field to make regression test stable
                MsAuthor = MsAuthor,
            });
        }

        public DocfxException ToException(Exception? innerException = null, bool isError = true)
        {
            return new DocfxException(isError ? WithLevel(ErrorLevel.Error) : this, innerException);
        }

        private class ErrorItem
        {
            [JsonProperty("message_severity")]
            public ErrorLevel MessageSeverity { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; } = string.Empty;

            [JsonProperty("message")]
            public string Message { get; set; } = string.Empty;

            [JsonProperty("file")]
            public PathString? File { get; set; }

            [JsonProperty("line")]
            public int Line { get; set; }

            [JsonProperty("end_line")]
            public int EndLine { get; set; }

            [JsonProperty("column")]
            public int Column { get; set; }

            [JsonProperty("end_column")]
            public int EndColumn { get; set; }

            [JsonProperty("log_item_type")]
            public string LogItemType { get; set; } = string.Empty;

            [JsonProperty("pull_request_only")]
            public bool? PullRequestOnly { get; set; }

            [JsonProperty("property_path")]
            public string? PropertyPath { get; set; }

            [JsonProperty("date_time")]
            public DateTime DateTime { get; set; }

            [JsonProperty("ms.author")]
            public string? MsAuthor { get; set; }
        }

        private class EqualityComparer : IEqualityComparer<Error>
        {
            public bool Equals(Error? x, Error? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.Level == y.Level &&
                       x.Code == y.Code &&
                       x.Message == y.Message &&
                       x.PropertyPath == y.PropertyPath &&
                       x.Source == y.Source &&
                       x.OriginalPath == y.OriginalPath &&
                       x.PullRequestOnly == y.PullRequestOnly;
            }

            public int GetHashCode(Error obj)
            {
                return HashCode.Combine(
                    obj.Level,
                    obj.Code,
                    obj.Message,
                    obj.PropertyPath,
                    obj.Source,
                    obj.OriginalPath,
                    obj.PullRequestOnly);
            }
        }
    }
}
