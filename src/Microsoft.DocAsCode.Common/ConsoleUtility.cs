// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
