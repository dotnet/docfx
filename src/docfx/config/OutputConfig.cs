// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class OutputConfig
    {
        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public readonly string Path = "_site";

        /// <summary>
        /// Gets whether to output JSON model.
        /// </summary>
        public readonly bool Json = false;

        /// <summary>
        /// Gets whether to include `.html` in urls.
        /// The default value is to generate an `index.html` for each article.
        /// </summary>
        public readonly bool UglifyUrl = false;

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public readonly bool CopyResources = true;
    }
}
