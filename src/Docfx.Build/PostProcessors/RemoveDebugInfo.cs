// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.Engine;

class RemoveDebugInfo : HtmlDocumentHandler
{
    private readonly string[] DebugInfoAttributes =
    [
        "sourceFile",
        "sourceStartLineNumber",
        "sourceEndLineNumber",
        "jsonPath",
        "data-raw-source",
        "nocheck",
    ];

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
        manifestItem.Metadata.Remove("rawTitle");
    }
}
