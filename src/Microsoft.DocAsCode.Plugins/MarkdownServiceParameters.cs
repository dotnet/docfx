// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Microsoft.DocAsCode.Plugins;

public class MarkdownServiceParameters
{
    public string BasePath { get; set; }
    public string TemplateDir { get; set; }
    public IReadOnlyDictionary<string, object> Extensions { get; set; } = ImmutableDictionary<string, object>.Empty;
    public ImmutableDictionary<string, string> Tokens { get; set; } = ImmutableDictionary<string, string>.Empty;
}
