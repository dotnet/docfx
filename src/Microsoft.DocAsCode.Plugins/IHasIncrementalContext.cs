// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    /// <summary>
    /// Declare some object has incremental build context.
    /// </summary>
    public interface IHasIncrementalContext
    {
        /// <summary>
        /// Get the hash of incremental context, if it is different from latest one then full build.
        /// </summary>
        /// <returns>the hash.</returns>
        string GetIncrementalContextHash();
    }
}
