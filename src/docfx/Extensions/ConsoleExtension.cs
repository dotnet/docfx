// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx;

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
