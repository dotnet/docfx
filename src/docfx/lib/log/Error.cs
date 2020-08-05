// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

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

        public PathString? OriginalPath { get; }

        public bool PullRequestOnly { get; }

        public Error(
            ErrorLevel level,
            string code,
            string message,
            SourceInfo? source = null,
            string? name = null,
            PathString? originalPath = null,
            bool pullRequestOnly = false)
        {
            Level = level;
            Code = code;
            Message = message;
            Source = source;
            Name = name;
            OriginalPath = originalPath;
            PullRequestOnly = pullRequestOnly;
        }

        public Error WithCustomRule(CustomRule customRule, bool? isCanonicalVersion = null)
        {
            var level = customRule.Severity ?? Level;
            level = isCanonicalVersion != null && customRule.CanonicalVersionOnly
                ? (isCanonicalVersion.Value ? level : ErrorLevel.Off)
                : level;

            if (level != ErrorLevel.Off && customRule.Exclude.Any())
            {
                foreach (var exclusion in customRule.Exclude)
                {
                    var matcher = GlobUtility.CreateGlobMatcher(exclusion);
                    if (matcher(OriginalPath ?? Source?.File?.Path ?? string.Empty))
                    {
                        level = ErrorLevel.Off;
                        break;
                    }
                }
            }

            return new Error(
                level,
                string.IsNullOrEmpty(customRule.Code) ? Code : customRule.Code,
                string.IsNullOrEmpty(customRule.AdditionalMessage) ? Message : $"{Message}{(Message.EndsWith('.') ? "" : ".")} {customRule.AdditionalMessage}",
                Source,
                Name,
                OriginalPath,
                customRule.PullRequestOnly);
        }

        public Error WithLevel(ErrorLevel level)
        {
            return level == Level ? this : new Error(level, Code, Message, Source, Name, OriginalPath, PullRequestOnly);
        }

        public Error WithOriginalPath(PathString? originalPath)
        {
            return originalPath == OriginalPath ? this : new Error(Level, Code, Message, Source, Name, originalPath, PullRequestOnly);
        }

        public Error WithSource(SourceInfo? source)
        {
            return new Error(Level, Code, Message, source, Name, OriginalPath, PullRequestOnly);
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
                Message,
                file,
                line,
                end_line,
                column,
                end_column,
                log_item_type = "user",
                pull_request_only = PullRequestOnly ? (bool?)true : null,
                date_time = DateTime.UtcNow,
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
                       x.Name == y.Name &&
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
                    obj.Name,
                    obj.Source,
                    obj.OriginalPath,
                    obj.PullRequestOnly);
            }
        }
    }
}
