// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        public static DfmEngineBuilder CreateBuilder(string baseDir)
        {
            // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
            return new DfmEngineBuilder(new Options() { Mangle = false, XHtml = true }, baseDir);
        }

        public static string Markup(string src, string path = null, ImmutableDictionary<string, string> tokens = null)
        {
            var engine = CreateBuilder(null).CreateDfmEngine(new DfmRenderer(tokens ?? new Dictionary<string, string>().ToImmutableDictionary()));
            return engine.Markup(src, path);
        }
    }
}
