// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;
    using System.Collections.Generic;

    using Markdig.Syntax;

    [Serializable]
    public class MarkdownMetadataItemModel
    {
        public string OPathString { get; set; }

        public int LineNumber { get; set; }

        public MarkdownDocument Value { get; set; }

        public MarkdownMetadataItemModel(MarkdownPropertyModel model, string file)
        {
            OPathString = model.PropertyName;
            LineNumber = model.PropertyNameSource.Line + 1;
            Value = CreateDocument(model.PropertyValue, file);
        }

        private MarkdownDocument CreateDocument(List<Block> blocks, string file)
        {
            var result = new MarkdownDocument();
            foreach (var block in blocks)
            {
                block.Parent?.Remove(block);
                result.Add(block);
            }
            result.SetData("filePath", file);
            return result;
        }
    }
}
