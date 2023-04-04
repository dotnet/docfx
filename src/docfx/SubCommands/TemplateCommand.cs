// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Exceptions;

namespace Microsoft.DocAsCode.SubCommands;

internal class TemplateCommand
{
    private const string DefaultOutputFolder = "_exported_templates";

    private string[] _templates;

    private TemplateCommandType _commandType;

    private ExportTemplateConfig _exportTemplateConfig = null;

    public void Exec(TemplateCommandOptions options)
    {
        if (options.Commands == null || !options.Commands.Any() || !Enum.TryParse(options.Commands.First(), true, out _commandType))
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
        }
        _templates = Directory.GetDirectories(Path.Combine(AppContext.BaseDirectory, "templates")).Select(Path.GetFileName).ToArray();

        switch (_commandType)
        {
            case TemplateCommandType.List:
                ExecListTemplate();
                break;
            case TemplateCommandType.Export:
                ExecExportTemplate();
                break;
        }
    }

    private void ExecListTemplate()
    {
        _templates.WriteLinesToConsole(ConsoleColor.White);
    }

    private void ExecExportTemplate()
    {
        var outputFolder = string.IsNullOrEmpty(_exportTemplateConfig.OutputFolder) ? DefaultOutputFolder : _exportTemplateConfig.OutputFolder;
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        var templates = _exportTemplateConfig.All ? _templates : _exportTemplateConfig.Templates;
        foreach (var template in templates)
        {
            Logger.LogInfo($"Exporting {template} to {outputFolder}");
            var manager = new TemplateManager(typeof(Docset).Assembly, Constants.EmbeddedTemplateFolderName, new List<string> { template }, null, null);
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
