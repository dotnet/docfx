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

        public FilePath? FilePath { get; }

        public int Line { get; }

        public int Column { get; }

        public int EndLine { get; }

        public int EndColumn { get; }

        public Error(ErrorLevel level, string code, string message, SourceInfo? source, string? name = null)
            : this(level, code, message, source?.File, source?.Line ?? 0, source?.Column ?? 0, source?.EndLine ?? 0, source?.EndColumn ?? 0, name)
        { }

        public Error(ErrorLevel level, string code, string message, FilePath? file = null, int line = 0, int column = 0, int endLine = 0, int endColumn = 0, string? name = null)
        {
            Level = level;
            Code = code;
            Message = message;
            FilePath = file;
            Line = line;
            Column = column;
            EndLine = endLine;
            EndColumn = endColumn;
            Name = name;
        }

        public Error WithCustomError(CustomError customError)
        {
            return new Error(
                customError.Severity ?? Level,
                string.IsNullOrEmpty(customError.Code) ? Code : customError.Code,
                string.IsNullOrEmpty(customError.AdditionalMessage) ? Message : $"{Message}{(Message.EndsWith('.') ? "" : ".")} {customError.AdditionalMessage}",
                FilePath,
                Line,
                Column,
                EndLine,
                EndColumn,
                Name);
        }

        public Error WithLevel(ErrorLevel level)
        {
            return new Error(level, Code, Message, FilePath, Line, Column, EndLine, EndColumn, Name);
        }

        public override string ToString() => ToString(Level, null);

        public string ToString(ErrorLevel level, SourceMap? sourceMap)
        {
            var message_severity = level;
            int line = Line;
            int end_line = EndLine;
            int column = Column;
            int end_column = EndColumn;
            var originalPath = FilePath is null ? null : sourceMap?.GetOriginalFilePath(FilePath);
            var file = originalPath == null ? FilePath?.Path : originalPath;
            var date_time = DateTime.UtcNow;
            var log_item_type = "user";

            return originalPath == null
                ? JsonUtility.Serialize(new { message_severity, log_item_type, Code, Message, file, line, end_line, column, end_column, date_time })
                : JsonUtility.Serialize(new { message_severity, log_item_type, Code, Message, file });
        }

        public DocfxException ToException(Exception? innerException = null, bool isError = true)
        {
            return new DocfxException(this, innerException, isError ? (ErrorLevel?)ErrorLevel.Error : null);
        }

        private class EqualityComparer : IEqualityComparer<Error>
        {
            public bool Equals(Error x, Error y)
            {
                return x.Level == y.Level &&
                       x.Code == y.Code &&
                       x.Message == y.Message &&
                       x.Name == y.Name &&
                       x.FilePath == y.FilePath &&
                       x.Line == y.Line &&
                       x.Column == y.Column;
            }

            public int GetHashCode(Error obj)
            {
                return HashCode.Combine(
                    obj.Level,
                    obj.Code,
                    obj.Message,
                    obj.Name,
                    obj.FilePath,
                    obj.Line,
                    obj.Column);
            }
        }
    }
}
