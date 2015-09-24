// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;

    public enum ResultLevel
    {
        Success,
        Verbose,
        Info,
        Warning,
        Error,
    }

    public class ParseResult
    {
        public static ParseResult SuccessResult = new ParseResult(ResultLevel.Success);
        public static ParseResult WarningResult = new ParseResult(ResultLevel.Warning);
        public static ParseResult ErrorResult = new ParseResult(ResultLevel.Error);

        public ResultLevel ResultLevel { get; set; }

        public string Message { get; set; }

        public string Phase { get; set; }

        public ParseResult(ResultLevel resultLevel, string message, params string[] arg)
        {
            ResultLevel = resultLevel;
            Message = string.Format(message, arg);
        }

        public ParseResult(ResultLevel resultLevel)
        {
            ResultLevel = resultLevel;
        }

        public void WriteToConsole()
        {
            if (string.IsNullOrEmpty(Message)) return;
            if (ResultLevel > ResultLevel.Info)
            {
                Console.Error.WriteLine(ToString());
            }
            else
            {
                Console.WriteLine(ToString());
            }
        }

        public static void WriteInfo(object message)
        {
            Console.Write(ResultLevel.Info + ": ");
            Console.WriteLine(message);
        }

        // optimize, prevent new string[0].
        public static void WriteToConsole(ResultLevel resultLevel, string message)
        {
            WriteToConsole(resultLevel, message, null);
        }

        public static void WriteToConsole(ResultLevel resultLevel, string message, params string[] arg)
        {
            var formatter = resultLevel + ": " + message;
            // TODO: add to input
            if (resultLevel == ResultLevel.Verbose) return;
            if (resultLevel > ResultLevel.Info)
            {
                if (arg == null || arg.Length == 0)
                {
                    // Incase there are {{}} inside the message
                    Console.Error.WriteLine(formatter);
                }
                else
                    Console.Error.WriteLine(formatter, arg);
            }
            else
            {
                if (arg == null || arg.Length == 0)
                {
                    // Incase there are {{}} inside the message
                    Console.WriteLine(formatter);
                }
                else
                    Console.WriteLine(formatter, arg);
            }
        }

        public override string ToString()
        {
            return ResultLevel.ToString() + ": " + Message;
        }
    }
}
