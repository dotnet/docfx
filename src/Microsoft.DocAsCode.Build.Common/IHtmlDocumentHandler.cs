// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    internal interface IHtmlDocumentHandler
    {
        void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
        Manifest Complete(Manifest manifest);
    }
}
