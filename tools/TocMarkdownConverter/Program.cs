using System;
using System.IO;

namespace TocMarkdownConverter
{
    internal sealed class Program
    {
        private static readonly string MarkdownExtension = ".md";

        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return 1;
            }

            var tocMarkdown = args[0];
            var extension = Path.GetExtension(tocMarkdown);
            if (!string.Equals(extension, MarkdownExtension, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Only support to convert toc markdown file.");
                return 1;
            }

            var tocYml = args.Length > 1 ? args[1] : null;
            try
            {
                tocYml = TocConverter.Convert(tocMarkdown, tocYml);
                Console.WriteLine($"Successfully convert {tocMarkdown} to {tocYml}.");

                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to convert {tocMarkdown} to {tocYml}, {ex.Message}");

                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine($"\t{AppDomain.CurrentDomain.FriendlyName} <path of toc markdown file> [path of generated toc yaml file]");
        }
    }
}