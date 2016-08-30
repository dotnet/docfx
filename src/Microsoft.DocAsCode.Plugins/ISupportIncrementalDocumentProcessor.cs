// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.IO;

    /// <summary>
    /// Declare a document processor can support incremental build.
    /// </summary>
    public interface ISupportIncrementalDocumentProcessor : IDocumentProcessor
    {
        /// <summary>
        /// Get the hash of incremental context, if it is different from latest one then full build.
        /// </summary>
        /// <returns>the hash.</returns>
        string GetIncrementalContextHash();
        /// <summary>
        /// Save intermediate model to stream.
        /// </summary>
        /// <param name="model">The model to save.</param>
        /// <param name="stream">The stream for saving.</param>
        void SaveIntermediateModel(FileModel model, Stream stream);
        /// <summary>
        /// Load intermediate model from stream.
        /// </summary>
        /// <param name="stream">The stream containing saved model.</param>
        /// <returns>The file model.</returns>
        FileModel LoadIntermediateModel(Stream stream);
    }
}
