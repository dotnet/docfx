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

        public string? Name { get; }

        public SourceInfo? Source { get; }

        public bool PullRequestOnly { get; }

        public Error(ErrorLevel level, string code, string message, SourceInfo? source = null, string? name = null, bool pullRequestOnly = false)
        {
            Level = level;
            Code = code;
            Message = message;
            Source = source;
            Name = name;
            PullRequestOnly = pullRequestOnly;
        }

        public Error WithCustomRule(CustomRule customRule, bool? isCanonicalVersion = null)
        {
            var level = customRule.Severity ?? Level;
            level = isCanonicalVersion != null && customRule.CanonicalVersionOnly
                ? (isCanonicalVersion.Value ? level : ErrorLevel.Off)
                : level;

            return new Error(
                level,
                string.IsNullOrEmpty(customRule.Code) ? Code : customRule.Code,
                string.IsNullOrEmpty(customRule.AdditionalMessage) ? Message : $"{Message}{(Message.EndsWith('.') ? "" : ".")} {customRule.AdditionalMessage}",
                Source,
                Name,
                customRule.PullRequestOnly);
        }

        public Error WithLevel(ErrorLevel level)
        {
            return new Error(level, Code, Message, Source, Name);
        }

        public override string ToString() => ToString(Level, null);

        public string ToString(ErrorLevel level, SourceMap? sourceMap)
        {
            var message_severity = level;
            var line = Source?.Line ?? 0;
            var end_line = Source?.EndLine ?? 0;
            var column = Source?.Column ?? 0;
            var end_column = Source?.EndColumn ?? 0;
            var originalPath = Source?.File is null ? null : sourceMap?.GetOriginalFilePath(Source.File);
            var file = originalPath == null ? Source?.File?.Path : originalPath;
            var date_time = DateTime.UtcNow;
            var log_item_type = "user";
            var pull_request_only = PullRequestOnly ? (bool?)true : null;

            return originalPath == null
                ? JsonUtility.Serialize(new
                {
                    message_severity,
                    log_item_type,
                    Code,
                    Message,
                    file,
                    line,
                    end_line,
                    column,
                    end_column,
                    pull_request_only,
                    date_time,
                })
                : JsonUtility.Serialize(new { message_severity, log_item_type, Code, Message, file, pull_request_only });
        }

        public DocfxException ToException(Exception? innerException = null, bool isError = true)
        {
            return new DocfxException(this, innerException, isError ? (ErrorLevel?)ErrorLevel.Error : null);
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
                       x.Name == y.Name &&
                       x.Source == y.Source &&
                       x.PullRequestOnly == y.PullRequestOnly;
            }

            public int GetHashCode(Error obj)
            {
                return HashCode.Combine(
                    obj.Level,
                    obj.Code,
                    obj.Message,
                    obj.Name,
                    obj.Source,
                    obj.PullRequestOnly);
            }
        }
    }
}
