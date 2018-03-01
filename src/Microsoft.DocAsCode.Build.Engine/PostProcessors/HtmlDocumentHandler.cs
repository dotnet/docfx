// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using HtmlAgilityPack;

    public abstract class HtmlDocumentHandler : IHtmlDocumentHandler
    {
        public HtmlPostProcessContext Context { get; private set; }
        public virtual void LoadContext(HtmlPostProcessContext context) { }
        protected abstract void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile, string outputFile);
        protected virtual Manifest PostHandleCore(Manifest manifest) => manifest;
        protected virtual Manifest PreHandleCore(Manifest manifest) => manifest;
        public virtual void SaveContext(HtmlPostProcessContext context) { }

        public void SetContext(HtmlPostProcessContext context)
        {
            Context = context;
        }

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
}
