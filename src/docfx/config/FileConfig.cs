// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal class FileConfig
    {
        /// <summary>
        /// Gets the include patterns of files.
        /// </summary>
        public readonly string[] Include;

        /// <summary>
        /// Gets the exclude patterns of files.
        /// </summary>
        public readonly string[] Exclude;

        public FileConfig(string[] include, string[] exclude)
        {
            Include = include ?? Array.Empty<string>();
            Exclude = exclude ?? Array.Empty<string>();
        }
    }
}
