// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;
    using System.IO;

    public interface IPostProcessorHost
    {
        /// <summary>
        /// Source file information
        /// </summary>
        IImmutableList<SourceFileInfo> SourceFileInfos { get; set; }

        /// <summary>
        /// Whether the post processor can be incremental.
        /// </summary>
        bool IsIncremental { get; set; }

        /// <summary>
        /// Load context info from last post processing.
        /// </summary>
        /// <returns>Stream to be read, return null when there is no last info.</returns>
        Stream LoadContextInfo();

        /// <summary>
        /// Save context information to current post processing.
        /// </summary>
        /// <returns>Stream to be written</returns>
        Stream SaveContextInfo();
    }
}
