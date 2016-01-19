// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Dfm;

    public class AzureMarked
    {
        private static readonly AzureEngineBuilder _builder = new AzureEngineBuilder(new Options() { Mangle = false });
        private static readonly DfmMarkdownRenderer _renderer = new DfmMarkdownRenderer();

        public static string Markup(string src)
        {
            var engine = _builder.CreateEngine(_renderer);
            return engine.Markup(src);
        }
    }
}
