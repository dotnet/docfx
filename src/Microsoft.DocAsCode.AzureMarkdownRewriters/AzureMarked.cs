// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Dfm;

    public class AzureMarked
    {
        private static readonly AzureEngineBuilder _builder = new AzureEngineBuilder(new Options() { Mangle = false });
        private static readonly AzureMarkdownRenderer _renderer = new AzureMarkdownRenderer();

        public static string Markup(string src, string path = null)
        {
            var engine = (MarkdownEngine)_builder.CreateEngine(_renderer);
            return engine.Mark(engine.Normalize(src), engine.Context.CreateContext(engine.Context.Variables.SetItem("path", path)));
        }
    }
}
