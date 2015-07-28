// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    
    public static class ConsoleExtension
    {
        private static Stack<ConsoleColor> colorStack = new Stack<ConsoleColor>();

        public static void WriteToConsole(this string text, ConsoleColor color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var foreColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = foreColor;
        }

        public static void WriteLineToConsole(this string text, ConsoleColor color)
        {
            if (string.IsNullOrEmpty(text)) return;
            var foreColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = foreColor;
        }

        public static void WriteLinesToConsole(this string[] text, ConsoleColor color)
        {
            if (text == null || text.Length == 0) return;
            foreach(var line in text)
            {
                line.WriteLineToConsole(color);
            }
        }
    }
}
