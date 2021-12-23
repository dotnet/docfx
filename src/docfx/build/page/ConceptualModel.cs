// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal class ConceptualModel
{
    public string? Conceptual { get; set; }

    public long? WordCount { get; set; }

    public SourceInfo<string?> Title { get; set; }

    public string RawTitle { get; set; } = "";
}
