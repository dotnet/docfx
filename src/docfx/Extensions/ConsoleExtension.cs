// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode;

internal static class ConsoleExtension
{
    public static void WriteToConsole(this string text, ConsoleColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        ConsoleUtility.Write(text, color);
    }

    public static void WriteLineToConsole(this string text, ConsoleColor color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        ConsoleUtility.WriteLine(text, color);
    }

    public static void WriteLinesToConsole(this string[] text, ConsoleColor color)
    {
        if (text == null || text.Length == 0)
        {
            return;
        }
        foreach (var line in text)
        {
            line.WriteLineToConsole(color);
        }
    }
}
