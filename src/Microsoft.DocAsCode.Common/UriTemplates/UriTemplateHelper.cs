// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Common;

public static class UriTemplate
{
    public static UriTemplate<T> Create<T>(string template, Func<string, T> func, Func<string, IUriTemplatePipeline<T>> pipelineProvider) =>
        UriTemplate<T>.Parse(template, func, pipelineProvider);
}
