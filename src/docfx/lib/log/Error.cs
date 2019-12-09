// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal class Error
    {
        public static readonly IEqualityComparer<Error> Comparer = new EqualityComparer();

        public ErrorLevel Level { get; }

        public string Code { get; }

        public string Message { get; }

        public FilePath FilePath { get; }

        public int Line { get; }

        public int Column { get; }

        public int EndLine { get; }

        public int EndColumn { get; }

        public Error(ErrorLevel level, string code, string message, SourceInfo source)
            : this(level, code, message, source?.File, source?.Line ?? 0, source?.Column ?? 0, source?.EndLine ?? 0, source?.EndColumn ?? 0)
        { }

        public Error(ErrorLevel level, string code, string message, FilePath file = null, int line = 0, int column = 0, int endLine = 0, int endColumn = 0)
        {
            Debug.Assert(!string.IsNullOrEmpty(code));
            Debug.Assert(Regex.IsMatch(code, "^[a-z0-9-]{5,32}$"), $"Error code '{code}' should only contain dash and letters in lowercase");
            Debug.Assert(!string.IsNullOrEmpty(message));

            Level = level;
            Code = code;
            Message = message;
            FilePath = file;
            Line = line;
            Column = column;
            EndLine = endLine;
            EndColumn = endColumn;
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
                EndColumn);
        }

        public Error WithLevel(ErrorLevel level)
        {
            return new Error(level, Code, Message, FilePath, Line, Column, EndLine, EndColumn);
        }

        public override string ToString() => ToString(Level);

        public string ToString(ErrorLevel level)
        {
            object[] payload = { level, Code, Message, FilePath?.Path, Line, Column };

            var i = payload.Length - 1;
            while (i >= 0 && (Equals(payload[i], null) || Equals(payload[i], "") || Equals(payload[i], 0) || Equals(payload[i], FileOrigin.Default)))
            {
                i--;
            }
            return JsonUtility.Serialize(payload.Take(i + 1));
        }

        public DocfxException ToException(Exception innerException = null)
        {
            return new DocfxException(this, innerException);
        }

        private class EqualityComparer : IEqualityComparer<Error>
        {
            public bool Equals(Error x, Error y)
            {
                return x.Level == y.Level &&
                       x.Code == y.Code &&
                       x.Message == y.Message &&
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
                    obj.FilePath,
                    obj.Line,
                    obj.Column);
            }
        }
    }
}
