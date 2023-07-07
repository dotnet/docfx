// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

internal class InternalManifestItem
{
    public string DocumentType { get; set; }

    /// <summary>
    /// relative path from docfx.json
    /// </summary>
    public string LocalPathFromRoot { get; set; }

    public string Key { get; set; }

    public string FileWithoutExtension { get; set; }

    public string Extension { get; set; }

    public string ResourceFile { get; set; }

    public string InputFolder { get; set; }

    public object Content { get; set; }

    public Dictionary<string, object> Metadata { get; set; }
}
