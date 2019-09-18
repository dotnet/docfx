// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    public enum FileOrigin
    {
        /// <summary>
        /// This file is coming from the main docset folder.
        /// </summary>
        Default = 0,

        /// <summary>
        /// This file is coming from a dependency repository.
        /// </summary>
        Dependency = 1,

        /// <summary>
        /// This file is coming from the fallback repository of a localized build.
        /// </summary>
        Fallback = 1 << 1,

        /// <summary>
        /// This file is a redirection file.
        /// </summary>
        Redirection = 1 << 2,

        /// <summary>
        /// This file is coming from template.
        /// </summary>
        Template = 1 << 3,
    }
}
