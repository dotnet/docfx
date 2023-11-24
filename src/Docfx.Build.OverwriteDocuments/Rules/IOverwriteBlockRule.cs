// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Syntax;

namespace Docfx.Build.OverwriteDocuments;

public interface IOverwriteBlockRule
{
    string TokenName { get; }

    bool Parse(Block block, out string value);
}
