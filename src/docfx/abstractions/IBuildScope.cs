// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents the initial build scope.
    /// </summary>
    internal interface IBuildScope
    {
        /// <summary>
        /// Gets all the files to build including fallback files.
        /// </summary>
        FilePath[] Files { get; }
    }
}
