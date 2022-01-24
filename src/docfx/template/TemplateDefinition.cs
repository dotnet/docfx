// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Docs.Build;

/// <summary>
/// Data model for an optional `template.json` file that describes the docfx template.
/// </summary>
internal class TemplateDefinition
{
    /// <summary>
    /// Gets the file glob patterns to copy as static assets.
    /// </summary>
    [JsonConverter(typeof(OneOrManyConverter))]
    public string[] Assets { get; init; } = Array.Empty<string>();
}
