// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Docfx.Common.EntityMergers;


/// <summary>
///   The placement for a merge.
/// </summary>
internal enum MergePlacement
{

    /// <summary>
    ///   The placement is not specified.
    /// </summary>
    None,

    /// <summary>
    ///   The override must be placed after the original content.
    /// </summary>
    After,

    /// <summary>
    ///   The override must be placed before the original content.
    /// </summary>
    Before,

    /// <summary>
    ///   The override must replace the original content.
    /// </summary>
    Replace
}
