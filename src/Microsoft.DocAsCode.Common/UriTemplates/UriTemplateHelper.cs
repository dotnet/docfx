// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Common;

public static class UriTemplate
{
    public static UriTemplate<T> Create<T>(string template, Func<string, T> func, Func<string, IUriTemplatePipeline<T>> pipelineProvider) =>
        UriTemplate<T>.Parse(template, func, pipelineProvider);
}
