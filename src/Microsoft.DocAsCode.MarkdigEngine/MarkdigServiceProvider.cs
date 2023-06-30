// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.MarkdigEngine;

public class MarkdigServiceProvider : IMarkdownServiceProvider
{
    public ICompositionContainer Container { get; init; }
    public Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> ConfigureMarkdig { get; init; }

    public IMarkdownService CreateMarkdownService(MarkdownServiceParameters parameters)
    {
        return new MarkdigMarkdownService(parameters, Container, ConfigureMarkdig);
    }
}
