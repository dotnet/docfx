// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    public interface IHtmlDocumentHandler
    {
        void LoadContext(HtmlPostProcessContext context);
        Manifest PreHandle(Manifest manifest);
        void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
        Manifest PostHandle(Manifest manifest);
        void SaveContext(HtmlPostProcessContext context);
    }
}
