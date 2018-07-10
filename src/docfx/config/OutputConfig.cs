// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class OutputConfig
    {
        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public readonly string Path = "_site";

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public readonly bool CopyResources = true;
    }
}
