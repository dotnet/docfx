// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.ComponentModel;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DocfxFlavoredMarked
    {
        private static DfmEngineBuilder _builder;
        private static readonly DfmRenderer _renderer = new DfmRenderer();

        public static string Markup(string src, string path = null)
        {
            var engine = GetBuilder().CreateDfmEngine(_renderer);
            return engine.Markup(src, path);
        }

        internal static void ClearBuilder()
        {
            _builder = null;
        }

        private static DfmEngineBuilder GetBuilder()
        {
            var result = _builder;
            if (result == null)
            {
                // TODO: currently disable mangle as a quick workaround for OP Build Service compatibility
                result = new DfmEngineBuilder(new Options() { Mangle = false });
                _builder = result;
            }
            return result;
        }
    }
}
