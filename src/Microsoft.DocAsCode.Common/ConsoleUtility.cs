// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Common;

public static class ConsoleUtility
{
    public static void Write(string message, ConsoleColor color)
    {
        try
        {
            if (color == ConsoleColor.White)
                Console.ResetColor();
            else
                Console.ForegroundColor = color;
            Console.Write(message);
        }
        finally
        {
            Console.ResetColor();
        }
    }

    public static void WriteLine(string message, ConsoleColor color)
    {

        try
        {
            if (color == ConsoleColor.White)
                Console.ResetColor();
            else
                Console.ForegroundColor = color;

            Console.WriteLine(message);
        }
        finally
        {
            Console.ResetColor();
        }
    }
}
