// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal enum OutputType
{
    /// <summary>
    /// HTML file after applying liquid template, including both content and the chrome.
    /// </summary>
    Html,

    /// <summary>
    /// The default JSON file before applying any templates.
    /// </summary>
    Json,

    /// <summary>
    /// Liquid JSON input format, same content as stored in docs document hosting service.
    /// </summary>
    PageJson,
}
