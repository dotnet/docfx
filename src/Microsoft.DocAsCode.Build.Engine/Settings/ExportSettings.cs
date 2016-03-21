// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;

    public class ExportSettings
    {
        public bool Export { get; set; }
        public string OutputFolder { get; set; }
        public string Extension { get; set; }
        public Func<string, string> PathRewriter { get; set; }

        public ExportSettings() { }

        public ExportSettings(ExportSettings settings)
        {
            Export = settings.Export;
            OutputFolder = settings.OutputFolder;
            Extension = settings.Extension;
            PathRewriter = settings.PathRewriter;
        }
    }
}
