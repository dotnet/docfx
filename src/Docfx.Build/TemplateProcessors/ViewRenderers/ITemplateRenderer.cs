// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine;

public interface ITemplateRenderer
{
    string Render(object model);

    IEnumerable<string> Dependencies { get; }

    string Path { get; }

    string Name { get; }
}
