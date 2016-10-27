// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using HtmlAgilityPack;

    public abstract class HtmlDocumentHandler : IHtmlDocumentHandler
    {
        public abstract void Handle(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
        public abstract Manifest PostHandle(Manifest manifest);
        public abstract Manifest PreHandle(Manifest manifest);

        public void HandleWithScopeWrapper(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile)
        {
            string phase = this.GetType().Name;
            using (new LoggerPhaseScope(phase, false))
            using (new PerformanceScope(phase, LogLevel.Verbose))
            {
                Handle(document, manifestItem, inputFile, outputFile);
            }
        }

        public Manifest PostHandleWithScopeWrapper(Manifest manifest)
        {
            string phase = this.GetType().Name;
            using (new LoggerPhaseScope(phase, false))
            using (new PerformanceScope(phase, LogLevel.Verbose))
            {
                return PostHandle(manifest);
            }
        }

        public Manifest PreHandleWithScopeWrapper(Manifest manifest)
        {
            string phase = this.GetType().Name;
            using (new LoggerPhaseScope(phase, false))
            using (new PerformanceScope(phase, LogLevel.Verbose))
            {
                return PreHandle(manifest);
            }
        }
    }
}
