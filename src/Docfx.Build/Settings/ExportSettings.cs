// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

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
