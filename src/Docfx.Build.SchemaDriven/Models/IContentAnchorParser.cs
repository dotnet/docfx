// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.SchemaDriven;

public interface IContentAnchorParser
{
    string Parse(string input);

    bool ContainsAnchor { get; }

    string Content { get; }
}
