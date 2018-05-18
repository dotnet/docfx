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
        /// Gets a value indicating whether build produces stable output for comparison in a diff tool.
        /// </summary>
        public readonly bool Stable;
    }
}
