// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdownMigrateTool
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode.Dfm;

    internal sealed class Program
    {
        private readonly DfmEngineBuilder _builder;

        public Program()
        {
            var option = DocfxFlavoredMarked.CreateDefaultOptions();
            option.LegacyMode = true;
            _builder = new DfmEngineBuilder(option);
        }

        private static void Main(string[] args)
        {
            if (args.Length == 1 && "-i".Equals(args[0], StringComparison.InvariantCultureIgnoreCase))
            {
                RunInteractiveMode();
                return;
            }

            if (args.Length >= 1 && args.Length <= 2 && File.Exists(args[0]))
            {
                Parse(args, out string input, out string output);
                new Program().MigrateFile(input, output);
                return;
            }
            PrintUsage();
        }

        private static void RunInteractiveMode()
        {
            string line;
            var p = new Program();
            while ((line = Console.ReadLine()) != null)
            {
                var args = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length > 0 && File.Exists(args[0]))
                {
                    Parse(args, out string input, out string output);
                    p.MigrateFile(input, output);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine($"Usage:");
            Console.WriteLine($"   {AppDomain.CurrentDomain.FriendlyName} <input file> [<output file>]");
            Console.WriteLine($"   or");
            Console.WriteLine($"   {AppDomain.CurrentDomain.FriendlyName} -i");
        }

        private static void Parse(string[] args, out string input, out string output)
        {
            input = args[0];
            if (args.Length == 2)
            {
                output = args[1];
            }
            else
            {
                output = input;
            }
        }

        private void MigrateFile(string inputFile, string outputFile)
        {
            var result = Convert(inputFile, File.ReadAllText(inputFile));
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            File.WriteAllText(outputFile, result);
            Console.WriteLine($"{inputFile} has been migrated to {outputFile}.");
        }

        private string Convert(string inputFile, string markdown)
        {
            var engine = _builder.CreateDfmEngine(new DfmMarkdownRenderer());
            return engine.Markup(markdown, inputFile);
        }
    }
}
