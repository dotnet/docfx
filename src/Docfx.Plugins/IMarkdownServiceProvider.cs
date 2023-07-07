// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Plugins;

public interface IMarkdownServiceProvider
{
    IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters);
}
