// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        public static DfmRenderer Renderer { get; } = new DfmRenderer();

        public static DfmEngineBuilder CreateBuilder()
        {
            // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
            return new DfmEngineBuilder(new Options() { Mangle = false });
        }

        public static string Markup(string src, string path = null)
        {
            var engine = CreateBuilder().CreateDfmEngine(Renderer);
            return engine.Markup(src, path);
        }
    }
}
