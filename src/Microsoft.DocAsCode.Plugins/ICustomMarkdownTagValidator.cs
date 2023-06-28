// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Plugins;

/// <summary>
/// Contract interface for custom validate tag in markdown
/// </summary>
public interface ICustomMarkdownTagValidator
{
    bool Validate(string tag);
}
