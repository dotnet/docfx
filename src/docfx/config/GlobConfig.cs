// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class GlobConfig<T>
    {
        /// <summary>
        /// Gets the include patterns of files.
        /// </summary>
        public string[] Include;

        /// <summary>
        /// Gets the exclude patterns of files.
        /// </summary>
        public string[] Exclude;

        /// <summary>
        /// Gets the value to apply to files.
        /// </summary>
        public T Value;

        public GlobConfig(string[] include, string[] exclude, T value)
        {
            Debug.Assert(value != null);

            Include = include ?? Array.Empty<string>();
            Exclude = exclude ?? Array.Empty<string>();
            Value = value;
        }
    }
}
