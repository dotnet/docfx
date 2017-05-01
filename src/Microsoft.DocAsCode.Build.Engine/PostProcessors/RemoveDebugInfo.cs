// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    public sealed class RemoveDebugInfo : HtmlDocumentHandler
    {
        private readonly string[] DebugInfoAttributes =
        {
            "sourceFile",
            "sourceStartLineNumber",
            "sourceEndLineNumber",
            "data-raw-source",
            "nocheck",
        };

        protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            foreach (var node in document.DocumentNode.Descendants())
            {
                if (!node.HasAttributes)
                {
                    continue;
                }
                foreach (var remove in DebugInfoAttributes)
                {
                    foreach (var attr in node.ChildAttributes(remove))
                    {
                        attr.Remove();
                    }
                }
            }
        }
    }
}
