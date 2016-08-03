// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        public static DfmEngineBuilder CreateBuilder(string baseDir) =>
            CreateBuilder(baseDir, null);

        public static DfmEngineBuilder CreateBuilder(string baseDir, string templateDir) =>
            // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
            new DfmEngineBuilder(new Options() { Mangle = false, XHtml = true }, baseDir, templateDir);

        public static string Markup(string src, string path = null, ImmutableDictionary<string, string> tokens = null, HashSet<string> dependency = null)
        {
            var engine = CreateBuilder(null).CreateDfmEngine(new DfmRenderer() { Tokens = tokens });
            return engine.Markup(src, path, dependency);
        }
    }
}
