// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    internal enum PackageFetchOptions
    {
        /// <summary>
        /// Default fetch option. For git repositories, this fetches full history.
        /// </summary>
        None = 0,

        /// <summary>
        /// Uses --depth 1 to fetch git repositories.
        /// </summary>
        DepthOne = 0b0001,

        /// <summary>
        /// Ignore the package resolved directory not existed error
        /// </summary>
        IgnoreDirectoryNonExisted = 0b0010,
    }
}
