// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public enum TemplateType
{
    Default,

    /// <summary>
    /// Primary template type means documents processed by this template will be responsible for hyperlink
    /// </summary>
    Primary,

    /// <summary>
    /// Auxiliary template type means documents processed by this template will not be referenced by other documents
    /// </summary>
    Auxiliary,
}
