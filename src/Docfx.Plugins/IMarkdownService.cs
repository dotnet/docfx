// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public interface IMarkdownService
{
    string Name { get; }

    MarkupResult Markup(string src, string path);
}
