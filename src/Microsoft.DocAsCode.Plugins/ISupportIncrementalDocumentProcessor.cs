// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.IO;

    /// <summary>
    /// Declare a document processor or a step can support incremental build.
    /// </summary>
    public interface ISupportIncrementalDocumentProcessor : IDocumentProcessor, ISupportIncrementalBuild
    {
        /// <summary>
        /// Save intermediate model to stream.
        /// </summary>
        /// <param name="model">The model to save.</param>
        /// <param name="stream">The stream for saving.</param>
        void SaveIntermediateModel(FileModel model, Stream stream);
        /// <summary>
        /// Load intermediate model from stream.
        /// </summary>
        /// <param name="stream">The stream contain saved model.</param>
        /// <returns>The file model.</returns>
        FileModel LoadIntermediateModel(Stream stream);
    }
}
