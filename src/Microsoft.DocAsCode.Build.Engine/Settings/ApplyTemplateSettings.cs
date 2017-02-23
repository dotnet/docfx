// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.IO;

    using Microsoft.DocAsCode.Plugins;

    public class ApplyTemplateSettings
    {
        private const string RawModelExtension = ".raw.json";
        private const string ViewModelExtension = ".view.json";
        private const string RawModelOutputFolderNameForDebug = "rawmodel";
        private const string ViewModelOutputFolderNameForDebug = "viewmodel";
        private static readonly string DefaultOutputFolderForDebug = Path.Combine(Path.GetTempPath(), "docfx");
        public static readonly ExportSettings DefaultRawModelExportSettings = new ExportSettings { Extension = RawModelExtension, PathRewriter = s => s + RawModelExtension };
        public static readonly ExportSettings DefaultViewModelExportSettings = new ExportSettings { Extension = ViewModelExtension, PathRewriter = s => s + ViewModelExtension };
        public string InputFolder { get; }
        public string OutputFolder { get; }
        public bool DebugMode { get; }
        public bool TransformDocument { get; set; } = true;
        public ExportSettings RawModelExportSettingsForDebug { get; set; } = new ExportSettings(DefaultRawModelExportSettings);
        public ExportSettings ViewModelExportSettingsForDebug { get; set; } = new ExportSettings(DefaultRawModelExportSettings);
        public ExportSettings RawModelExportSettings { get; set; } = new ExportSettings(DefaultRawModelExportSettings);
        public ExportSettings ViewModelExportSettings { get; set; } = new ExportSettings(DefaultViewModelExportSettings);
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
}
