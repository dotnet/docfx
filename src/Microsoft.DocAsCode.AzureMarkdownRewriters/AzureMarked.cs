// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMarked
    {
        private static readonly AzureEngineBuilder _builder = new AzureEngineBuilder(new Options() { Mangle = false });
        private static readonly AzureMarkdownRenderer _renderer = new AzureMarkdownRenderer();

        public static string Markup(
            string src,
            string path = null,
            IReadOnlyDictionary<string, AzureFileInfo> azureFileInfoMapping = null,
            IReadOnlyDictionary<string, AzureVideoInfo> azureVideoInfoMapping = null)
        {
            var engine = (MarkdownEngine)_builder.CreateEngine(_renderer);
            var context = engine.Context;
            if (path != null)
            {
                context = engine.Context.CreateContext(engine.Context.Variables.SetItem("path", path));
            }

            if (azureFileInfoMapping != null)
            {
                context = context.CreateContext(context.Variables.SetItem("azureFileInfoMapping", azureFileInfoMapping));
            }

            if (azureVideoInfoMapping != null)
            {
                context = context.CreateContext(context.Variables.SetItem("azureVideoInfoMapping", azureVideoInfoMapping));
            }

            return engine.Mark(MarkdownEngine.Normalize(src), context);
        }
    }
}
