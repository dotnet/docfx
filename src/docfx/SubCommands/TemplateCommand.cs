// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Exceptions;

    internal sealed class TemplateCommand : ISubCommand
    {
        private static readonly string[] ExistingTemplates = new string[] { "default", "iframe.html", "statictoc" };
        private const string DefaultOutputFolder = "_exported_templates";
        private readonly TemplateCommandOptions _options;

        private readonly TemplateCommandType _commandType;

        private readonly ExportTemplateConfig _exportTemplateConfig = null;
        public bool AllowReplay => false;

        public TemplateCommand(TemplateCommandOptions options)
        {
            _options = options;
            if (options.Commands == null || options.Commands.Count == 0 || !Enum.TryParse(options.Commands[0], true, out _commandType))
            {
                throw new InvalidOptionException("Neither 'list' nor 'export' is found. You must specify a command type.");
            }
            switch (_commandType)
            {
                case TemplateCommandType.Export:
                    _exportTemplateConfig = new ExportTemplateConfig
                    {
                        All = options.All,
                        OutputFolder = options.OutputFolder,
                        Templates = options.Commands.Skip(1).ToArray()
                    };
                    if (_exportTemplateConfig.Templates.Length == 0)
                    {
                        _exportTemplateConfig.All = true;
                    }
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
            $"{Environment.NewLine}Existing embedded templates are:".WriteLineToConsole(ConsoleColor.Gray);
            ExistingTemplates.Select(s => "\t" + s).ToArray().WriteLinesToConsole(ConsoleColor.White);
        }

        private void ExecExportTemplate()
        {
            var outputFolder = string.IsNullOrEmpty(_exportTemplateConfig.OutputFolder) ? DefaultOutputFolder : _exportTemplateConfig.OutputFolder;
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var templates = _exportTemplateConfig.All ? ExistingTemplates : _exportTemplateConfig.Templates;
            foreach (var template in templates)
            {
                Logger.LogInfo($"Exporting {template} to {outputFolder}");
                var manager = new TemplateManager(this.GetType().Assembly, "Template", new List<string> { template }, null, null);
                if (manager.TryExportTemplateFiles(Path.Combine(outputFolder, template)))
                {
                    Logger.LogInfo($"{template} is exported to {outputFolder}");
                }
                else
                {
                    Logger.LogWarning($"{template} is not an embedded template.");
                }
            }
        }

        private enum TemplateCommandType
        {
            List,
            Export,
        }

        private sealed class ExportTemplateConfig
        {
            public string[] Templates { get; set; }

            public string OutputFolder { get; set; }

            public bool All { get; set; }
        }
    }
}
