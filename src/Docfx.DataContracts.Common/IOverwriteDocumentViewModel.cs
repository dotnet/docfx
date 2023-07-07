// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.DataContracts.Common;

public interface IOverwriteDocumentViewModel
{
    /// <summary>
    /// The uid for this overwrite document, as defined in YAML header
    /// </summary>
    string Uid { get; set; }

    /// <summary>
    /// The markdown content from the overwrite document
    /// </summary>
    string Conceptual { get; set; }

    /// <summary>
    /// The details for current overwrite document, containing the start/end line numbers, file path, and git info.
    /// </summary>
    SourceDetail Documentation { get; set; }
}
