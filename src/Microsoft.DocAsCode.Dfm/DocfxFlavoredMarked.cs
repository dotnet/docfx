// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        public static Options CreateDefaultOptions() =>
            new Options() { Mangle = false, XHtml = true };

        public static DfmEngineBuilder CreateBuilder(string baseDir) =>
            CreateBuilder(baseDir, null);

        public static DfmEngineBuilder CreateBuilder(string baseDir, string templateDir) =>
            CreateBuilder(baseDir, null, null);

        public static DfmEngineBuilder CreateBuilder(string baseDir, string templateDir, Options options) =>
            new DfmEngineBuilder(options ?? CreateDefaultOptions(), baseDir, templateDir, null);

        public static DfmEngineBuilder CreateBuilder(string baseDir, string templateDir, Options options, IReadOnlyList<string> fallbackFolders) =>
            new DfmEngineBuilder(options ?? CreateDefaultOptions(), baseDir, templateDir, fallbackFolders);

        public static string Markup(string src, string path = null, ImmutableDictionary<string, string> tokens = null, HashSet<string> dependency = null)
        {
            var engine = CreateBuilder(null).CreateDfmEngine(new DfmRenderer() { Tokens = tokens });
            return engine.Markup(src, path, dependency);
        }

        public static string Markup(string baseDir, string templateDir, Options options, string src, string path = null, ImmutableDictionary<string, string> tokens = null, HashSet<string> dependency = null)
        {
            var engine = CreateBuilder(baseDir, templateDir, options ?? CreateDefaultOptions()).CreateDfmEngine(new DfmRenderer() { Tokens = tokens });
            return engine.Markup(src, path, dependency);
        }

        public static string Markup(string baseDir, string src, IReadOnlyList<string> fallbackFolders, string path = null, ImmutableDictionary<string, string> tokens = null, HashSet<string> dependency = null)
        {
            var engine = CreateBuilder(baseDir, null, null, fallbackFolders).CreateDfmEngine(new DfmRenderer() { Tokens = tokens });
            return engine.Markup(src, path, dependency);
        }
    }
}
