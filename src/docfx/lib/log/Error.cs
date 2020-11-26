// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
                ms_author = MsAuthor,
                date_time = DateTime.UtcNow, // Leave data_time as the last field to make regression test stable
            }).Replace("\"ms_author\"", "\"ms.author\"");
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
