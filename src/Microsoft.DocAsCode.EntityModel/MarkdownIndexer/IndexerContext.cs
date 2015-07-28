// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.MarkdownIndexer
{
    using System.Collections.Generic;

    public class IndexerContext
    {
        public ApiReferenceModel ExternalApiIndex { get; set; } 

        public string[] ApiIndexFiles { get; set; }

        /// <summary>
        /// The source path for the markdown file, source path is to calculate file's remote repository file link
        /// </summary>
        public string MarkdownFileSourcePath { get; set; }

        public string CurrentWorkingDirectory { get; set; }

        public string TargetFolder { get; set; }

        /// <summary>
        /// The output target path for the markdown file, target path is the real path for in-site references
        /// </summary>
        public string MarkdownFileTargetPath { get; set; }

        public string MarkdownContent { get; set; }

        public string MarkdownMapFileOutputFolder { get; set; }

        public string ApiMapFileOutputFolder { get; set; }

        public string ReferenceOutputFolder { get; set; }
    }
}
