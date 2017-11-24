// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdownMigrateTool
{
    using System;

    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            var opt = new CommandLineOptions();
            try
            {
                if (opt.Parse(args))
                {
                    MarkdownMigrateTool.Migrate(opt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
