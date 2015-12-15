// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    public enum ResultLevel
    {
        Success,
        Info,
        Warning,
        Error,
    }

    public class ParseResult : ILogItem
    {
        public static ParseResult SuccessResult = new ParseResult(ResultLevel.Success);
        public static ParseResult WarningResult = new ParseResult(ResultLevel.Warning);
        public static ParseResult ErrorResult = new ParseResult(ResultLevel.Error);

        public ResultLevel ResultLevel { get; set; }

        public string Message { get; set; }

        public string Phase { get; set; }

        public string File { get; set; }

        public string Line { get; set; }

        public LogLevel LogLevel => GetLogLevel(ResultLevel);

        public ParseResult(ResultLevel resultLevel, string message)
        {
            ResultLevel = resultLevel;
            Message = message;
        }

        public ParseResult(ResultLevel resultLevel)
        {
            ResultLevel = resultLevel;
        }

        private LogLevel GetLogLevel(ResultLevel level)
        {
            switch (level)
            {
                case ResultLevel.Success:
                    return LogLevel.Verbose;
                case ResultLevel.Info:
                    return LogLevel.Info;
                case ResultLevel.Warning:
                    return LogLevel.Warning;
                case ResultLevel.Error:
                    return LogLevel.Error;
                default:
                    throw new NotSupportedException(level.ToString());
            }
        }
    }
}
