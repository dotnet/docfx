// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Utility
{
    using System;

    public static class ConsoleUtility
    {
        public static void WriteToConsoleWithColor(Action write, ConsoleColor color)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));
            var originalColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                write();
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
}
