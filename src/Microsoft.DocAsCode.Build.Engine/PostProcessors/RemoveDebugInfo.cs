// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    public sealed class RemoveDebugInfo : HtmlDocumentHandler
    {
        protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            foreach (var node in document.DocumentNode.Descendants())
            {
                if (!node.HasAttributes)
                {
                    continue;
                }
                foreach (var attr in node.ChildAttributes("sourceFile"))
                {
                    attr.Remove();
                }
                foreach (var attr in node.ChildAttributes("sourceStartLineNumber"))
                {
                    attr.Remove();
                }
                foreach (var attr in node.ChildAttributes("sourceEndLineNumber"))
                {
                    attr.Remove();
                }
                foreach (var attr in node.ChildAttributes("data-raw-source"))
                {
                    attr.Remove();
                }
                foreach (var attr in node.ChildAttributes("nocheck"))
                {
                    attr.Remove();
                }
            }
        }
    }
}
