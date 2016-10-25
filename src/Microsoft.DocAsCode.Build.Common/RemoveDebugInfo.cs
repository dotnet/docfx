// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    public class RemoveDebugInfo : IHtmlDocumentHandler
    {
        public Manifest PreHandle(Manifest manifest)
        {
            return manifest;
        }

        public void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
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
            }
        }

        public Manifest PostHandle(Manifest manifest)
        {
            return manifest;
        }
    }
}
