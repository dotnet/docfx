// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs
{
    internal class OutputConfig
    {
        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public string Path { get; } = "_site";

        /// <summary>
        /// Gets the absolute build log output path, or path relative to <see cref="OutputConfig.Path"/>.
        /// </summary>
        public string LogPath { get; } = "build.log";

        /// <summary>
        /// Gets a value indicating whether build produces stable output for comparison in a diff tool.
        /// </summary>
        public bool Stable { get; }
    }
}
