// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{

    using MarkdownLite;

    public class DocfxFlavoredMarked
    {
        // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
        private static readonly DfmEngineBuilder _builder = new DfmEngineBuilder(new Options() { Mangle = false });
        private static readonly DfmRenderer _renderer = new DfmRenderer();

        public static string Markup(string src, string path = null)
        {
            var engine = _builder.CreateEngine(_renderer);
            return engine.Markup(src, path);
        }
    }
}
