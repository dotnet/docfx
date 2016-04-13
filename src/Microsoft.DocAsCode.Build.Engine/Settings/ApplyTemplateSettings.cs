// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System.IO;

    public class ApplyTemplateSettings
    {
        private const string RawModelExtension = ".raw.json";
        private const string ViewModelExtension = ".view.json";
        private const string RawModelOutputFolderForDebug = "obj/rawmodel";
        private const string ViewModelOutputFolderForDebug = "obj/viewmodel";
        public static readonly ExportSettings DefaultRawModelExportSettings = new ExportSettings { Extension = RawModelExtension, PathRewriter = s => s + RawModelExtension };
        public static readonly ExportSettings DefaultViewModelExportSettings = new ExportSettings { Extension = ViewModelExtension, PathRewriter = s => s + ViewModelExtension };
        public static readonly ExportSettings RawModelExportSettingsForDebug = new ExportSettings(DefaultRawModelExportSettings) { Export = true, OutputFolder = RawModelOutputFolderForDebug };
        public static readonly ExportSettings ViewModelExportSettingsForDebug = new ExportSettings(DefaultViewModelExportSettings) { Export = true, OutputFolder = ViewModelOutputFolderForDebug };
        public string InputFolder { get; }
        public string OutputFolder { get; }
        public bool TransformDocument { get; set; } = true;
        public ExportSettings RawModelExportSettings { get; set; } = new ExportSettings(DefaultRawModelExportSettings);
        public ExportSettings ViewModelExportSettings { get; set; } = new ExportSettings(DefaultViewModelExportSettings);

        public ApplyTemplateSettings(string inputFolder, string outputFolder)
        {
            InputFolder = inputFolder;
            OutputFolder = outputFolder;
            RawModelExportSettings.OutputFolder = outputFolder;
            ViewModelExportSettings.OutputFolder = outputFolder;
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
