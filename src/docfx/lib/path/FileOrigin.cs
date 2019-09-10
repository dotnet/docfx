// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum FileOrigin
    {
        /// <summary>
        /// This file is coming from the main docset folder.
        /// </summary>
        Default,

        /// <summary>
        /// This file is coming from a dependency repository.
        /// </summary>
        Dependency,

        /// <summary>
        /// This file is coming from the fallback repository of a localized build.
        /// </summary>
        Fallback,

        /// <summary>
        /// This file is a redirection file.
        /// </summary>
        Redirection,

        /// <summary>
        /// This file is coming from template.
        /// </summary>
        Template,
    }
}
