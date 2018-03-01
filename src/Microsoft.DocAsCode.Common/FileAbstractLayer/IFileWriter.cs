// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System.IO;

    /// <summary>
    /// File writer.
    /// </summary>
    public interface IFileWriter
    {
        /// <summary>
        /// Create a file with logical file path.
        /// </summary>
        /// <param name="file">logical file path</param>
        /// <returns>file stream</returns>
        Stream Create(RelativePath file);
        /// <summary>
        /// Copy a file to logical file path.
        /// </summary>
        /// <param name="sourceFileName">Source file.</param>
        /// <param name="destFileName">Dest file (logical file path).</param>
        void Copy(PathMapping sourceFileName, RelativePath destFileName);
        /// <summary>
        /// Create a reader to read files in output.
        /// </summary>
        /// <returns>A file reader.</returns>
        IFileReader CreateReader();
    }
}
