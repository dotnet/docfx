// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        public static DfmRenderer Renderer { get; } = new DfmRenderer();

        public static DfmEngineBuilder CreateBuilder(string baseDir, IDictionary<string, object> tokens)
        {
            // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
            return new DfmEngineBuilder(new Options() { Mangle = false, XHtml = true }, tokens, baseDir);
        }

        public static string Markup(string src, string path = null, Dictionary<string, object> tokens = null)
        {
            var engine = CreateBuilder(null, tokens ?? new Dictionary<string, object>()).CreateDfmEngine(Renderer);
            return engine.Markup(src, path);
        }
    }
}
