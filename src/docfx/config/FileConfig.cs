// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal sealed class FileConfig
    {
        private static readonly string[] s_defaultContentInclude = new[] { "docs/**/*.{md,yml,json}" };
        private static readonly string[] s_defaultContentExclude = new[] { "_site/**/*", "localization/**/*" };

        /// <summary>
        /// Gets the include patterns of files.
        /// </summary>
        public readonly string[] Include = s_defaultContentInclude;

        /// <summary>
        /// Gets the exclude patterns of files.
        /// </summary>
        public readonly string[] Exclude = s_defaultContentExclude;
    }
}
