// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;

#nullable enable

namespace Microsoft.DocAsCode;

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
