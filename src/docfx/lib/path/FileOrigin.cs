// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

public enum FileOrigin
{
    /// <summary>
    /// Represents an external, non-content file source like config, template, etc..
    /// </summary>
    External,

    /// <summary>
    /// Represents a content file from the main docset.
    /// </summary>
    Main,

    /// <summary>
    /// Represents a content file from a dependency repository.
    /// </summary>
    Dependency,

    /// <summary>
    /// Represents a content file from the fallback repository of a localized build.
    /// </summary>
    Fallback,

    /// <summary>
    /// Represents a redirection file.
    /// </summary>
    Redirection,

    /// <summary>
    /// Represents a content file generated from code.
    /// </summary>
    Generated,
}
