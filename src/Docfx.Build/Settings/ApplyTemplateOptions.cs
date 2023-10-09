// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

[Flags]
public enum ApplyTemplateOptions
{
    None = 0,
    ExportRawModel = 1,
    ExportViewModel = 2,
    TransformDocument = 4,
    All = ExportRawModel | ExportViewModel | TransformDocument
}
