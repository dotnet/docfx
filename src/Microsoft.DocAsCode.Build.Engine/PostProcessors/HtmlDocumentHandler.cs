// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using HtmlAgilityPack;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public abstract class HtmlDocumentHandler : IHtmlDocumentHandler
{
    protected abstract void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
    protected virtual Manifest PostHandleCore(Manifest manifest) => manifest;
    protected virtual Manifest PreHandleCore(Manifest manifest) => manifest;

    public void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
    {
        string phase = GetType().Name;
        using (new LoggerPhaseScope(phase))
        {
            HandleCore(document, manifestItem, inputFile, outputFile);
        }
    }

    public Manifest PostHandle(Manifest manifest)
    {
        string phase = GetType().Name;
        using (new LoggerPhaseScope(phase))
        {
            return PostHandleCore(manifest);
        }
    }

    public Manifest PreHandle(Manifest manifest)
    {
        string phase = GetType().Name;
        using (new LoggerPhaseScope(phase))
        {
            return PreHandleCore(manifest);
        }
    }
}
