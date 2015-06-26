namespace docfx
{
    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;
    using System;
    using System.Diagnostics;
    using System.IO;

    public static class Constants
    {
        public static Func<string, string> GetIndexFilePathFunc = new Func<string, string>(s => Path.Combine(s, "index.yml"));
        public const string ConfigFileName = "xdoc.json";
        public const string WebsiteReferenceFolderName = "_ref_"; // Current OutputFolder
        public const string DefaultRootOutputFolderPath = "xdoc";
        public const string DefaultMetadataOutputFolderName = "_api_";
        public const string DefaultConceputalOutputFolderName = ""; // Current OutputFolder
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            Options options;
            var result = TryGetOptions(args, out options);

            if (!string.IsNullOrEmpty(result.Message)) result.WriteToConsole();
            if (result.ResultLevel == ResultLevel.Error) return 1;

            result = Exec(options);
            if (!string.IsNullOrEmpty(result.Message)) result.WriteToConsole();
            if (result.ResultLevel == ResultLevel.Error) return 1;
            if (result.ResultLevel == ResultLevel.Warning) return 2;
            return 0;
        }

        internal static ParseResult TryGetOptions(string[] args, out Options options)
        {
            options = new Options();

            string invokedVerb = null;
            object invokedVerbInstance = null;
            if(args.Length == 0)
            {
                // If no args, search for xdoc.json in current directory
                // Add this additional check as CommandLine.Parser throws NULL exception in this case
                options.Projects = new System.Collections.Generic.List<string> { Constants.ConfigFileName };
                return ParseResult.SuccessResult;
            }

            if (!Parser.Default.ParseArguments(args, options, (s, o) =>
            {
                invokedVerb = s;
                invokedVerbInstance = o;
            }))
            {
                if (!Parser.Default.ParseArguments(args, options))
                {
                    var text = HelpTextGenerator.GetHelpMessage(options);
                    return new ParseResult(ResultLevel.Error, text);
                }
                else
                {
                    return ParseResult.SuccessResult;
                }
            }
            else
            {
                try
                {
                    options.CurrentSubCommand = (SubCommandType)Enum.Parse(typeof(SubCommandType), invokedVerb, true);
                }
                catch
                {
                    return new ParseResult(ResultLevel.Error, "{0} subcommand is not currently supported.", invokedVerb);
                }
            }

            return ParseResult.SuccessResult;
        }

        internal static ParseResult Exec(Options options)
        {
            if (options.CurrentSubCommand == null)
            {
                if (options.Projects == null || options.Projects.Count == 0)
                {
                    // If no args, search for xdoc.json in current directory
                    options.Projects = new System.Collections.Generic.List<string> { Constants.ConfigFileName };

                    ParseResult.WriteToConsole(ResultLevel.Warning, "No projects are specified, try loading {0} config file.", Constants.ConfigFileName);
                }
                // Consider website as the default option
                options.WebsiteVerb = new WebsiteSubOptions(options);
                return SubCommandFactory.GetCommand(SubCommandType.Website).Exec(options);
            }
            else
            {
                var command = SubCommandFactory.GetCommand(options.CurrentSubCommand.Value);
                return command.Exec(options);
            }
        }
    }
}
