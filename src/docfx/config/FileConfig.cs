// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class FileConfig
    {
        private static readonly string[] s_defaultFileInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultFileExclude = Array.Empty<string>();

        /// <summary>
        /// Gets the include patterns of files to be build.
        /// </summary>
        public readonly string[] Include = s_defaultFileInclude;

        /// <summary>
        /// Gets the exclude patterns of files to be build.
        /// </summary>
        public readonly string[] Exclude = s_defaultFileExclude;
    }
}
