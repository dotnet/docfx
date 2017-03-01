// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    /// <summary>
    /// File writer.
    /// </summary>
    public interface ISupportRandomFileWriter
    {
        /// <summary>
        /// Create a random file name.
        /// </summary>
        /// <returns>A random file name.</returns>
        string CreateRandomFileName();
        /// <summary>
        /// Create a random file.
        /// </summary>
        /// <returns>A tuple of random file name and stream.</returns>
        Tuple<string, Stream> CreateRandomFile();
    }
}
