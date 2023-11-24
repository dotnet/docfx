// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;

#nullable enable

namespace Docfx;

/// <summary>
/// Provides options to be used with <see cref="Docset.Build(string, BuildOptions)"/>.
/// </summary>
public class BuildOptions
{
    /// <summary>
    /// Configures the markdig markdown pipeline.
    /// </summary>
    public Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder>? ConfigureMarkdig { get; init; }
}
