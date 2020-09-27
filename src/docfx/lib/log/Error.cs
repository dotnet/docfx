// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class Error
    {
        public static readonly IEqualityComparer<Error> Comparer = new EqualityComparer();

        public ErrorLevel Level { get; }

        public string Code { get; }

        public string Message { get; }

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

        private Error(
            ErrorLevel level,
            string code,
            string message,
            object?[] messageArguments,
            SourceInfo? source,
            string? propertyPath,
            PathString? originalPath,
            bool pullRequestOnly)
        {
            Level = level;
            Code = code;
            Message = message;
            MessageArguments = messageArguments;
            Source = source;
            PropertyPath = propertyPath;
            OriginalPath = originalPath;
            PullRequestOnly = pullRequestOnly;
        }

        public Error WithCustomRule(CustomRule customRule, bool? enabled = null)
        {
            var level = customRule.Severity ?? Level;

            if (level != ErrorLevel.Off && customRule.ExcludeMatches(OriginalPath ?? Source?.File?.Path ?? string.Empty))
            {
                level = ErrorLevel.Off;
            }

            if (enabled != null && !enabled.Value)
            {
                level = ErrorLevel.Off;
            }

            var message = Message;

            if (!string.IsNullOrEmpty(customRule.Message))
            {
                try
                {
                    message = string.Format(customRule.Message, MessageArguments);
                }
                catch (FormatException)
                {
                    message += "ERROR: custom message format is invalid, e.g., too many parameters {n}.";
                }
            }

            message = string.IsNullOrEmpty(customRule.AdditionalMessage) ?
                message : $"{message}{(message.EndsWith('.') ? "" : ".")} {customRule.AdditionalMessage}";

            return new Error(
                level,
                string.IsNullOrEmpty(customRule.Code) ? Code : customRule.Code,
                message,
                MessageArguments,
                Source,
                PropertyPath,
                OriginalPath,
                customRule.PullRequestOnly);
        }

        public Error WithLevel(ErrorLevel level)
        {
            return level == Level ? this : new Error(level, Code, Message, MessageArguments, Source, PropertyPath, OriginalPath, PullRequestOnly);
        }

        public Error WithOriginalPath(PathString? originalPath)
        {
            return originalPath == OriginalPath ? this : new Error(Level, Code, Message, MessageArguments, Source, PropertyPath, originalPath, PullRequestOnly);
        }

        public Error WithSource(SourceInfo? source)
        {
            return new Error(Level, Code, Message, MessageArguments, source, PropertyPath, OriginalPath, PullRequestOnly);
        }

        public Error WithPropertyPath(string? propertyPath)
        {
            return new Error(Level, Code, Message, MessageArguments, Source, propertyPath, OriginalPath, PullRequestOnly);
        }

        public override string ToString()
        {
            var file = OriginalPath ?? Source?.File?.Path;
            var source = OriginalPath is null ? Source : null;
            var line = source?.Line ?? 0;
            var end_line = source?.EndLine ?? 0;
            var column = source?.Column ?? 0;
            var end_column = source?.EndColumn ?? 0;

            return JsonUtility.Serialize(new
            {
                message_severity = Level,
                Code,
                message = Message,
                file,
                line,
                end_line,
                column,
                end_column,
                log_item_type = "user",
                pull_request_only = PullRequestOnly ? (bool?)true : null,
                property_path = PropertyPath,
                date_time = DateTime.UtcNow, // Leave data_time as the last field to make regression test stable
            });
        }

        public DocfxException ToException(Exception? innerException = null, bool isError = true)
        {
            return new DocfxException(isError ? WithLevel(ErrorLevel.Error) : this, innerException);
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
