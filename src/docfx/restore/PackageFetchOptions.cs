// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    [Flags]
    internal enum PackageFetchOptions
    {
        /// <summary>
        /// Default fetch option.
        /// </summary>
        None = 0,

        /// <summary>
        /// Include full commit history if the package is git.
        /// Git contributor calculate depend on git commit history.
        /// </summary>
        FullHistory = 0b0001,

        /// <summary>
        /// Don't fail if the package does not exist.
        /// Fallback branch for localized builds are typically optional.
        /// </summary>
        Optional = 0b0010,
    }
}
