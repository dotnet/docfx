// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// File reader.
    /// </summary>
    public interface IFileReader
    {
        /// <summary>
        /// Convert a logical file path to a physical file path
        /// </summary>
        /// <param name="file">Logical file path.</param>
        /// <returns>A path mapping.</returns>
        PathMapping? FindFile(RelativePath file);
        /// <summary>
        /// Get all files in this reader.
        /// </summary>
        /// <returns>A set of logical file path (from working folder).</returns>
        IEnumerable<RelativePath> EnumerateFiles();
        /// <summary>
        /// Get expected physical paths.
        /// </summary>
        /// <param name="file">Logical file path.</param>
        /// <returns>Expected physical paths.</returns>
        IEnumerable<string> GetExpectedPhysicalPath(RelativePath file);
    }
}
