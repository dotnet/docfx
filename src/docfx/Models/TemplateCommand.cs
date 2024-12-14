// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Docfx.Build.Engine;
using Docfx.Common;
using Spectre.Console.Cli;

namespace Docfx;

internal class TemplateCommand
{
    public class ListCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            foreach (var path in Directory.GetDirectories(GetTemplateBaseDirectory()))
                Console.WriteLine(Path.GetFileName(path));
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
                    Directory.GetDirectories(GetTemplateBaseDirectory())
                        .Select(Path.GetFileName)
                        .ToArray()
                    : options.Templates;

                foreach (var template in templates)
                {
                    var manager = new TemplateManager([template], null, null);
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

    private static string GetTemplateBaseDirectory()
    {
        if (DataContracts.Common.Constants.Switches.IsDotnetToolsMode)
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../templates"));

        return Path.Combine(AppContext.BaseDirectory, "templates");
    }
}
