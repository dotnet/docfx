// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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