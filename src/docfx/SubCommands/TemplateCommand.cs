// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Exceptions;

    internal sealed class TemplateCommand : ISubCommand
    {
        private static readonly string[] ExistingTemplates = new string[] { "default" };
        private const string DefaultOutputFolder = "_exported_templates";
        private readonly TemplateCommandOptions _options;

        private readonly TemplateCommandType _commandType;

        private readonly ExportTemplateConfig _exportTemplateConfig = null;
        private readonly ListTemplateConfig _listTemplateConfig = null;
        public TemplateCommand(TemplateCommandOptions options)
        {
            _options = options;
            if (options.Commands == null || options.Commands.Count == 0 || !Enum.TryParse(options.Commands[0], true, out _commandType))
            {
                throw new InvalidOptionException("Neither 'list' nor 'export' is found");
            }
            switch (_commandType)
            {
                case TemplateCommandType.List:
                    _listTemplateConfig = new ListTemplateConfig
                    {
                        All = options.All,
                    };
                    break;
                case TemplateCommandType.Export:
                    _exportTemplateConfig = new ExportTemplateConfig
                    {
                        All = options.All,
                        OutputFolder = options.OutputFolder,
                        Templates = options.Commands.Skip(1)
                    };
                    break;
                default:
                    break;
            }
        }

        public void Exec(SubCommandRunningContext context)
        {
            switch (_commandType)
            {
                case TemplateCommandType.List:
                    ExecListTemplate();
                    break;
                case TemplateCommandType.Export:
                    ExecExportTemplate();
                    break;
                default:
                    break;
            }
        }

        private void ExecListTemplate()
        {
            // TODO: dynamically load...
            $"{Environment.NewLine}Existing embeded templates are:".WriteLineToConsole(ConsoleColor.Gray);
            ExistingTemplates.Select(s => "\t" + s).ToArray().WriteLinesToConsole(ConsoleColor.White);
        }

        private void ExecExportTemplate()
        {
            var outputFolder = string.IsNullOrEmpty(_exportTemplateConfig.OutputFolder) ? DefaultOutputFolder : _exportTemplateConfig.OutputFolder;
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            List<string> templates = (_exportTemplateConfig.All ? ExistingTemplates : _exportTemplateConfig.Templates).ToList();
            foreach (var template in templates)
            {
                Logger.LogInfo($"Exporting {template} to {outputFolder}");
                var manager = new TemplateManager(this.GetType().Assembly, "Template", new List<string> { template }, null, null);
                manager.TryExportTemplateFiles(Path.Combine(outputFolder, template));
                Logger.LogInfo($"{template} is exported to {outputFolder}");
            }
        }

        private enum TemplateCommandType
        {
            List,
            Export,
        }

        private sealed class ExportTemplateConfig
        {
            public IEnumerable<string> Templates { get; set; }

            public string OutputFolder { get; set; }

            public bool All { get; set; }
        }

        private sealed class ListTemplateConfig
        {
            public bool All { get; set; }
        }
    }
}
