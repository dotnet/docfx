// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public interface ISupportIncrementalPostProcessor
    {
        /// <summary>
        /// Get or set post processor host
        /// </summary>
        IPostProcessorHost PostProcessorHost { get; set; }

        /// <summary>
        /// Get the hash of incremental context, skip incremental if it is different from latest one.
        /// </summary>
        /// <returns>the hash.</returns>
        string GetIncrementalContextHash();
    }
}