// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{

    using MarkdownLite;
    using System.Collections.Generic;

    public class DocfxFlavoredMarked
    {

        private static readonly DfmEngineBuilder _builder = new DfmEngineBuilder(new Options());
        private static readonly DfmRenderer _renderer = new DfmRenderer();

        public static string Markup(string src, string path = null)
        {
            var engine = _builder.CreateEngine(_renderer);
            return engine.Markup(src, path);
        }
    }
}
