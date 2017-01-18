// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.IO;

    public interface IPostProcessorHost
    {
        /// <summary>
        /// Load context info from last post processing.
        /// </summary>
        /// <returns>Stream to be read, Return null when there is no last info.</returns>
        Stream LoadContextInfo();

        /// <summary>
        /// Save context information to current post processing.
        /// </summary>
        /// <returns>Stream to be written</returns>
        Stream SaveContextInfo();
    }
}
