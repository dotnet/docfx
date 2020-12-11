// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class New
    {
        private static readonly string s_templatePath = Path.Combine(AppContext.BaseDirectory, "data", "new");

        public static bool Run(string type, CommandLineOptions options)
        {
            if (type == "." || !Directory.Exists(Path.Combine(s_templatePath, type)))
            {
                ShowTemplates();
                return false;
            }

            if (options.Force)
            {
                CreateFromTemplate(type, options, dryRun: false);
                return false;
            }

            if (CreateFromTemplate(type, options, dryRun: true))
            {
                CreateFromTemplate(type, options, dryRun: false);
                return false;
            }

            return true;
        }

        private static void ShowTemplates()
        {
            var width = 20;

            Console.WriteLine("usage: docfx new [<template>]");
            Console.WriteLine();
            Console.WriteLine("Template".PadRight(width, ' ') + "Description");

            try
            {
                Console.WriteLine("".PadRight(Console.BufferWidth - 4, '-'));
            }
            catch
            {
                // Console.BufferWidth sometimes throw
            }

            foreach (var template in Directory.GetDirectories(s_templatePath))
            {
                var name = Path.GetFileName(template);
                var description = File.ReadAllText(Path.Combine(template, "__description")).Trim();

                Console.WriteLine(name.PadRight(width, ' ') + description);
                Console.WriteLine();
            }
        }

        private static bool CreateFromTemplate(string type, CommandLineOptions options, bool dryRun)
        {
            var output = options.Output ?? ".";
            var overwriteFiles = new List<string>();

            foreach (var file in Directory.GetFiles(Path.Combine(s_templatePath, type)))
            {
                if (Path.GetFileName(file).StartsWith("__"))
                {
                    continue;
                }

                var target = Path.GetRelativePath(Path.Combine(s_templatePath, type), file);
                var targetFullPath = Path.GetFullPath(Path.Combine(output, target));

                if (dryRun)
                {
                    if (File.Exists(targetFullPath))
                    {
                        overwriteFiles.Add(target);
                    }
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath) ?? ".");
                    File.Copy(file, targetFullPath, overwrite: options.Force);
                }
            }

            if (dryRun)
            {
                if (overwriteFiles.Count <= 0)
                {
                    return true;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Creating this template will make changes to existing files:");

                foreach (var overwriteFile in overwriteFiles)
                {
                    Console.WriteLine("  " + overwriteFile);
                }

                Console.WriteLine();
                Console.WriteLine("Rerun the command and pass --force to accept and create.");
                Console.ResetColor();
                return false;
            }

            Console.WriteLine($"The template \"{type}\" was created successfully.");
            Console.WriteLine();

            Console.WriteLine($"Restoring dependencies in \"{output}\"");
            return Restore.Run(output, options);
        }
    }
}
