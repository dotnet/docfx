// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    /// <summary>
    /// Declare a build step can support incremental build.
    /// </summary>
    public interface ISupportIncrementalBuildStep : IDocumentBuildStep
    {
        /// <summary>
        /// Get the hash of incremental context, if it is different from latest one then full build.
        /// </summary>
        /// <returns>the hash.</returns>
        string GetIncrementalContextHash();
        /// <summary>
        /// Check each file, when incremental context hash is same.
        /// </summary>
        /// <param name="fileAndType">the file and type information</param>
        /// <returns>Can use incremental build for this file.</returns>
        bool CanIncrementalBuild(FileAndType fileAndType);
        /// <summary>
        /// Get dependency types to register
        /// </summary>
        /// <returns>dependency types to register</returns>
        IEnumerable<DependencyType> GetDependencyTypesToRegister();
    }
}
