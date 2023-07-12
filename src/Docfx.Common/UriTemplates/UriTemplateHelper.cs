// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Common;

public static class UriTemplate
{
    public static UriTemplate<T> Create<T>(string template, Func<string, T> func, Func<string, IUriTemplatePipeline<T>> pipelineProvider) =>
        UriTemplate<T>.Parse(template, func, pipelineProvider);
}
