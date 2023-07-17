// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Docfx.MarkdigEngine.Extensions;

public interface IMarkdownObjectRewriterProvider
{
    ImmutableArray<IMarkdownObjectRewriter> GetRewriters();
}
