// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.Engine;

public interface IHtmlDocumentHandler
{
    Manifest PreHandle(Manifest manifest);
    void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
    Manifest PostHandle(Manifest manifest);
}
