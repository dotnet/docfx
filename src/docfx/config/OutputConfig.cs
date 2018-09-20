// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal sealed class OutputConfig
    {
        public const int DefaultMaxErrors = 1000;

        /// <summary>
        /// Gets the build output directory. Could be absolute or relative.
        /// </summary>
        public readonly string Path = "_site";

        /// <summary>
        /// Gets whether to output JSON model.
        /// </summary>
        public readonly bool Json = false;

        /// <summary>
        /// Gets whether to use ugly url or pretty url when <see cref="Json"/> is set to false.
        ///  - Pretty url:      a.md --> a/index.html
        ///  - Ugly url:        a.md --> a.html
        /// </summary>
        public readonly bool UglifyUrl = false;

        /// <summary>
        /// Gets whether resources are copied to output.
        /// </summary>
        public readonly bool CopyResources = true;

        /// <summary>
        /// Gets the maximum errors or warnings to output.
        /// </summary>
        public readonly int MaxErrors = DefaultMaxErrors;
    }
}
