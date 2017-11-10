// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMarked
    {
        private static readonly AzureEngineBuilder _builder = new AzureEngineBuilder(new Options() { Mangle = false, LegacyMode = true });
        private static readonly AzureMarkdownRenderer _renderer = new AzureMarkdownRenderer();

        public static string Markup(
            string src,
            string path = null,
            IReadOnlyDictionary<string, AzureFileInfo> azureMarkdownFileInfoMapping = null,
            IReadOnlyDictionary<string, AzureVideoInfo> azureVideoInfoMapping = null,
            IReadOnlyDictionary<string, AzureFileInfo> azureResourceFileInfoMapping = null)
        {
            var engine = (MarkdownEngine)_builder.CreateEngine(_renderer);
            var context = engine.Context;
            if (path != null)
            {
                context = engine.Context.CreateContext(engine.Context.Variables.SetItem("path", path));
            }

            if (azureMarkdownFileInfoMapping != null && azureMarkdownFileInfoMapping.Count != 0)
            {
                context = context.CreateContext(context.Variables.SetItem("azureMarkdownFileInfoMapping", azureMarkdownFileInfoMapping));
            }

            if (azureVideoInfoMapping != null && azureVideoInfoMapping.Count != 0)
            {
                context = context.CreateContext(context.Variables.SetItem("azureVideoInfoMapping", azureVideoInfoMapping));
            }

            if (azureResourceFileInfoMapping != null && azureResourceFileInfoMapping.Count != 0)
            {
                context = context.CreateContext(context.Variables.SetItem("azureResourceFileInfoMapping", azureResourceFileInfoMapping));
            }

            return engine.Mark(SourceInfo.Create(MarkdownEngine.Normalize(src), path), context);
        }
    }
}
