// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal static class Logger
    {
        public static void Error(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        public static void Warn(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        public static void Info(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        public static void Verbose(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        public static void Diagnostic(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        private static void Log(string message, string file = "", int line = 0, int column = 0, string code = "") { }

        private static void Log(LogItem item)
        {

        }
    }
}
