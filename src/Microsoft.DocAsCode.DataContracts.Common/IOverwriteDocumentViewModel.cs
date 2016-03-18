// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DataContracts.Common
{
    public interface IOverwriteDocumentViewModel
    {
        /// <summary>
        /// The uid for this overwrite document, as defined in YAML header
        /// </summary>
        string Uid { get; set; }

        /// <summary>
        /// The markdown content from the overwrite document
        /// </summary>
        string Conceptual { get; set; }

        /// <summary>
        /// The details for current overwrite document, containing the start/end line numbers, file path, and git info.
        /// </summary>
        SourceDetail Documentation { get; set; }
    }
}
