// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

internal class TemplateCommand
{
    public class ListCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "templates"))
                .Select(Path.GetFileName)
                .ToArray()
                .WriteLinesToConsole(ConsoleColor.White);

            return 0;
        }
    }

    public class ExportCommand : Command<ExportCommand.Options>
    {
        [Description("Export existing template")]
        internal class Options : CommandSettings
        {
            [Description("Template name.")]
            [CommandArgument(0, "[template]")]
            public string[] Templates { get; set; }

            [Description("If specified, all the available templates will be exported.")]
            [CommandOption("-a|--all")]
            public bool All { get; set; }

            [Description("Specify the output folder path for the exported templates")]
            [CommandOption("-o|--output")]
            public string OutputFolder { get; set; }
        }

        public override int Execute(CommandContext context, Options options)
        {
            return CommandHelper.Run(() =>
            {
                var outputFolder = string.IsNullOrEmpty(options.OutputFolder) ? "_exported_templates" : options.OutputFolder;
                Directory.CreateDirectory(outputFolder);

                var templates = options.All || options.Templates is null || options.Templates.Length == 0 ?
                    Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "templates"))
                        .Select(Path.GetFileName)
                        .ToArray()
                    : options.Templates;

                foreach (var template in templates)
                {
                    var manager = new TemplateManager(new List<string> { template }, null, null);
                    if (manager.TryExportTemplateFiles(Path.Combine(outputFolder, template)))
                    {
                        Logger.LogInfo($"{template} is exported to {outputFolder}");
                    }
                    else
                    {
                        Logger.LogWarning($"Cannot find template {template}.");
                    }
                }
            });
        }
    }
}
