// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class ApplyTemplateSettings
{
    private const string RawModelExtension = ".raw.json";
    private const string ViewModelExtension = ".view.json";
    private const string RawModelOutputFolderNameForDebug = "rawmodel";
    private const string ViewModelOutputFolderNameForDebug = "viewmodel";
    private static readonly string DefaultOutputFolderForDebug = Path.Combine(Path.GetTempPath(), "docfx");
    public static readonly ExportSettings DefaultRawModelExportSettings = new() { Extension = RawModelExtension, PathRewriter = s => s + RawModelExtension };
    public static readonly ExportSettings DefaultViewModelExportSettings = new() { Extension = ViewModelExtension, PathRewriter = s => s + ViewModelExtension };
    public string InputFolder { get; }
    public string OutputFolder { get; }
    public bool DebugMode { get; }
    public bool TransformDocument { get; set; } = true;
    public ExportSettings RawModelExportSettingsForDebug { get; set; } = new(DefaultRawModelExportSettings);
    public ExportSettings ViewModelExportSettingsForDebug { get; set; } = new(DefaultRawModelExportSettings);
    public ExportSettings RawModelExportSettings { get; set; } = new(DefaultRawModelExportSettings);
    public ExportSettings ViewModelExportSettings { get; set; } = new(DefaultViewModelExportSettings);
    public ICustomHrefGenerator HrefGenerator { get; set; }

    public ApplyTemplateSettings(string inputFolder, string outputFolder) : this(inputFolder, outputFolder, null, false)
    { }

    public ApplyTemplateSettings(string inputFolder, string outputFolder, string debugOutputFolder, bool debugMode)
    {
        InputFolder = inputFolder;
        OutputFolder = outputFolder;
        RawModelExportSettings.OutputFolder = outputFolder;
        ViewModelExportSettings.OutputFolder = outputFolder;
        DebugMode = debugMode;
        var rootFolderForDebug = debugOutputFolder ?? DefaultOutputFolderForDebug;
        RawModelExportSettingsForDebug.OutputFolder = Path.Combine(rootFolderForDebug, RawModelOutputFolderNameForDebug);
        ViewModelExportSettingsForDebug.OutputFolder = Path.Combine(rootFolderForDebug, ViewModelOutputFolderNameForDebug);
    }

    public ApplyTemplateOptions Options
    {
        get
        {
            ApplyTemplateOptions options = ApplyTemplateOptions.None;
            if (TransformDocument) options |= ApplyTemplateOptions.TransformDocument;
            if (RawModelExportSettings.Export) options |= ApplyTemplateOptions.ExportRawModel;
            if (ViewModelExportSettings.Export) options |= ApplyTemplateOptions.ExportViewModel;
            return options;
        }
    }
}
